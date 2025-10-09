using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Microsoft.IO;

namespace System.Text.Json.Stream;

/// <summary>
/// A high-performance API for forward-only, read-only stream access to the UTF-8 encoded JSON text with conditional deserialization of json objects.
/// </summary>
public sealed class Utf8JsonAsyncStreamReader : IUtf8JsonAsyncStreamReader
{
    #region fields

    /// <summary>
    /// The minimum buffer size to use when reading from the stream.
    /// </summary>
    private readonly int _minimumBufferSize;
    
    /// <summary>
    /// The pipe reader used to read from the underlying stream.
    /// </summary>
    private readonly PipeReader _reader;

    /// <summary>
    /// The number of bytes consumed from the current buffer.
    /// </summary>
    private int _bytesConsumed;
    
    /// <summary>
    /// Indicates whether the reader has finished reading the stream.
    /// </summary>
    private bool _isFinished;
    
    /// <summary>
    /// Indicates whether the end of the stream has been reached.
    /// </summary>
    private bool _endOfStream;
    
    /// <summary>
    /// The current buffer containing data read from the stream.
    /// </summary>
    private ReadOnlySequence<byte> _buffer;
    
    /// <summary>
    /// The current state of the JSON reader.
    /// </summary>
    private JsonReaderState _jsonReaderState;

    /// <summary>
    /// Indicates whether the reader is currently buffering data for deserialization.
    /// </summary>
    private bool _isBuffering;
    
    /// <summary>
    /// The starting index of the buffer when buffering began.
    /// </summary>
    private int _bufferingStartIndex;

    /// <summary>
    /// The pipe writer used to write buffered data during deserialization.
    /// </summary>
    private PipeWriter? _writer;

    /// <summary>
    /// The raw bytes of the current JSON value (lazy-allocated).
    /// </summary>
    internal byte[]? RawValueBytes;

    /// <summary>
    /// Length of the value in bytes (internal so helpers can access it).
    /// </summary>
    internal int _valueLength;

        
    /// <summary>
    /// Reusable options for creating <see cref="PipeWriter"/> instances that leave the underlying stream open.
    /// </summary>
    private static readonly StreamPipeWriterOptions _writerOptions = new(leaveOpen: true);

    /// <summary>
    /// RecyclableMemoryStreamManager for pooling MemoryStream instances.
    /// Reduces Gen2 GC pressure by reusing streams instead of allocating new ones.
    /// </summary>
    private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();

        #endregion

    #region Properties

    /// <summary>
    /// The number of bytes read from the stream.
    /// </summary>
    public int BytesConsumed { get; private set; }

    /// <summary>
    /// The last processed JSON token in the UTF-8 encoded JSON text.
    /// </summary>
    public JsonTokenType TokenType { get; private set; } = JsonTokenType.None;

    
   /// <summary>
   /// The last processed value in the UTF-8 encoded JSON text.
   /// </summary>
    public object? Value => this.GetValue();

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a new <see cref="Utf8JsonAsyncStreamReader"/> instance.
    /// </summary>
    /// <param name="stream">The stream that the <see cref="Utf8JsonAsyncStreamReader"/> will wrap.</param>
    /// <param name="minimumBufferSize">The minimum buffer size to use when renting memory from the pool. The default value is 8192 bytes.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the underlying stream open after the <see cref="Utf8JsonAsyncStreamReader" /> completes; <see langword="false" /> to close it. The default is <see langword="false" />.</param>
    public Utf8JsonAsyncStreamReader(IO.Stream stream, int minimumBufferSize = -1, bool leaveOpen = false)
    {
        _minimumBufferSize = minimumBufferSize == -1 ? 1024 * 8 : minimumBufferSize;
        _reader = PipeReader.Create(stream, new StreamPipeReaderOptions(null, _minimumBufferSize, _minimumBufferSize, leaveOpen));
    }

    #endregion

    #region Methods

    /// <summary>Asynchronously reads a sequence of bytes.</summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see langword="default" />.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.ValueTask`1" /> representing the asynchronous read operation state. <see langword="false" /> if finished, <see langword="true" /> if there is more to read.</returns>
    /// <exception cref="JsonStreamException">
    /// Thrown when the JSON is invalid, a token is incomplete, or the buffer is undersized for the current JSON structure.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
    {
        // check if finished
        if (_isFinished)
        {
            TokenType = JsonTokenType.None;
            return false;
        }

        // we need to read more from the stream
        if (TokenType == JsonTokenType.None || !JsonReader(_endOfStream))
        {
            // store stream buffer/chunk if we are wanting to Deserialize the Json object
            if (_isBuffering)
            {
                WriteToBufferStream();

                // reset the buffer start tracking
                _bufferingStartIndex = 0;
            }

            // move the start of the buffer past what has already been consumed
            if (_bytesConsumed > 0) _reader.AdvanceTo(_buffer.GetPosition(_bytesConsumed));

            // top up the buffer stream
            ReadResult readResult = await _reader
                .ReadAtLeastAsync(_minimumBufferSize, cancellationToken)
                .ConfigureAwait(false);

            // reset to new stream buffer segment
            _bytesConsumed = 0;
            _buffer = readResult.Buffer;
            _endOfStream = readResult.IsCompleted;

            // check for any issues
            if (_buffer.Length - _bytesConsumed > 0 && !JsonReader(_endOfStream))
                throw new JsonStreamException("Invalid Json or incomplete token or buffer undersized");
        }

        // we have reached the end of the stream
        if (_endOfStream && _bytesConsumed == _buffer.Length) _isFinished = true;

        // inform stream buffer read state
        return !_isFinished;
    }

    /// <summary>
    /// Reads the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
    /// The Stream will be read to end of current branch.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
    /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
    /// <param name="options">Options to control the behavior during reading.</param>
    /// <param name="cancellationToken">
    /// The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <exception cref="System.ArgumentNullException"></exception>
    /// <exception cref="JsonStreamException">
    /// Thrown when the JSON stream is invalid, a token is incomplete, or the buffer is undersized for the current JSON structure.
    /// </exception>
    /// <exception cref="JsonException">
    /// The JSON is invalid,
    /// <typeparamref name="TValue"/> is not compatible with the JSON,
    /// or when there is remaining data in the Stream.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="TValue"/> or its serializable members.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<TValue?> DeserializeAsync<TValue>(JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        // move to start of object or array
        while (TokenType != JsonTokenType.StartObject && TokenType != JsonTokenType.StartArray && !cancellationToken.IsCancellationRequested)
            await ReadAsync(cancellationToken).ConfigureAwait(false);

        // RecyclableMemoryStream instead of MemoryStream
        // This pools and reuses stream instances, reducing Gen2 GC pressure
        using MemoryStream stream = _memoryStreamManager.GetStream();
        _writer = PipeWriter.Create(stream, _writerOptions);

        // fill temp stream with json object
        if (!await GetJsonObjectAsync(stream, cancellationToken).ConfigureAwait(false))
            return default;

        // deserialize object from temp stream
        TValue? result = await JsonSerializer
            .DeserializeAsync<TValue>(stream, options, cancellationToken)
            .ConfigureAwait(false);

        // temp stream is released and object returned
        return result;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    void IDisposable.Dispose()
    {
        Dispose(true);
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Utf8JsonAsyncStreamReader"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    public void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        
        _reader.Complete();
    }

    #region Internal Methods

    /// <summary>
    /// Reads the next JSON token from the buffer.
    /// </summary>
    /// <param name="isFinalBlock">Indicates whether this is the final block of data to read.</param>
    /// <returns><see langword="true"/> if a token was successfully read; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool JsonReader(bool isFinalBlock)
    {
        // reference the current position in the buffer
        ReadOnlySequence<byte> bytes = _buffer.Slice(_bytesConsumed);

        // read the next json token
        Utf8JsonReader reader = new(bytes, isFinalBlock, _jsonReaderState);

        bool result = reader.Read();

        int bytesConsumedByReader = (int)reader.BytesConsumed;
        _bytesConsumed += bytesConsumedByReader;   // within buffer window
        BytesConsumed += bytesConsumedByReader;    // within stream

        _jsonReaderState = reader.CurrentState;

        // nothing to read
        if (!result)
            return false;

        // store token
        TokenType = reader.TokenType;

        // Copy value bytes immediately (no lazy materialization)
        ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
        RawValueBytes = new byte[span.Length];
        span.CopyTo(RawValueBytes);
        _valueLength = span.Length;

        // success
        return true;
    }

    /// <summary>
    /// Reads a complete JSON object or array asynchronously and writes it to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write the JSON object data to.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The value of the TResult parameter contains <see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<bool> GetJsonObjectAsync(MemoryStream stream, CancellationToken cancellationToken)
    {
        // loop through data until end of object is found
        int depth = 0;

        // we are buffering all reads on the buffer stream
        _isBuffering = true;
        _bufferingStartIndex = _bytesConsumed - 1;

        // walk the json object tree until we have the complete json object
        while (!cancellationToken.IsCancellationRequested)
        {
            if (TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                depth++;
            else if (TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                depth--;

            if (depth == 0)
                break;

            await ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        // overflow
        WriteToBufferStream();

        // flush all writes
        await _writer!.CompleteAsync().ConfigureAwait(false);
        
        _isBuffering = false;

        // operation cancelled remotely
        if (cancellationToken.IsCancellationRequested)
            return false;

        // Point to beginning of the memory stream
        stream.Seek(0, SeekOrigin.Begin);

        // success
        return true;
    }

    /// <summary>
    /// Writes the current buffer data to the buffering stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteToBufferStream()
    {
        int bytes = _bytesConsumed - _bufferingStartIndex;
        
        if (bytes <= 0) return;
        
        ReadOnlySequence<byte> slice = _buffer.Slice(_bufferingStartIndex, bytes);
        slice.CopyTo(_writer!.GetSpan(bytes));
        _writer.Advance(bytes);
    }

    #endregion

    #endregion
}
