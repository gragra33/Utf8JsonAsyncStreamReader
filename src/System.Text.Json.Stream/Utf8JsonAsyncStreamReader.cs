using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Stream;

/// <summary>
/// A high-performance API for forward-only, read-only stream access to the UTF-8 encoded JSON text with conditional deserialization of json objects.
/// </summary>
public sealed class Utf8JsonAsyncStreamReader : IUtf8JsonAsyncStreamReader
{
    #region fields

    private readonly int _minimumBufferSize;
    private readonly PipeReader _reader;

    private int _bytesConsumed;
    private bool _isFinished;
    private bool _endOfStream;
    private ReadOnlySequence<byte> _buffer;
    private JsonReaderState _jsonReaderState;

    private bool _isBuffering;
    private int _bufferingStartIndex;

    private PipeWriter? _writer;

    internal byte[]? RawValueBytes;

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
    public object? Value => this.GetValue(); //{ get; private set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a new <see cref="Utf8JsonAsyncStreamReader"/> instance.
    /// </summary>
    /// <param name="stream">The stream that the <see cref="Utf8JsonAsyncStreamReader"/> will wrap.</param>
    /// <param name="minimumBufferSize">The minimum buffer size to use when renting memory from the <paramref name="pool" />. The default value is 4096.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the underlying stream open after the <see cref="Utf8JsonAsyncStreamReader" /> completes; <see langword="false" /> to close it. The default is <see langword="false" />.</param>
    public Utf8JsonAsyncStreamReader(IO.Stream stream, int minimumBufferSize = -1, bool leaveOpen = false)
    {
        this._minimumBufferSize = minimumBufferSize == -1 ? 1024 * 8 : minimumBufferSize;
        this._reader = PipeReader.Create(stream, new StreamPipeReaderOptions(null, this._minimumBufferSize, this._minimumBufferSize, leaveOpen));
    }

    #endregion

    #region Methods

    /// <summary>Asynchronously reads a sequence of bytes.</summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see langword="default" />.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.ValueTask`1" /> representing the asynchronous read operation state. <see langword="false" /> if finished, <see langword="true" /> if there is more to read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
    {
        // check if finished
        if (this._isFinished)
        {
            this.TokenType = JsonTokenType.None;
            return false;
        }

        // we need to read more from the stream
        if (this.TokenType == JsonTokenType.None || !this.JsonReader(this._endOfStream))
        {
            // store stream buffer/chunk if we are wanting to Deserialize the Json object
            if (this._isBuffering)
            {
                this.WriteToBufferStream();

                // reset the buffer start tracking
                this._bufferingStartIndex = 0;
            }

            // move the start of the buffer past what has already been consumed
            if (this._bytesConsumed > 0) this._reader.AdvanceTo(this._buffer.GetPosition(this._bytesConsumed));

            // top up the buffer stream
            ReadResult readResult = await this._reader
                .ReadAtLeastAsync(this._minimumBufferSize, cancellationToken)
                .ConfigureAwait(false);

            // reset to new stream buffer segment
            this._bytesConsumed = 0;
            this._buffer = readResult.Buffer;
            this._endOfStream = readResult.IsCompleted;

            // check for any issues
            if (this._buffer.Length - this._bytesConsumed > 0 && !this.JsonReader(this._endOfStream))
                throw new Exception("Invalid Json or incomplete token or buffer undersized");
        }

        // we have reached the end of the stream
        if (this._endOfStream && this._bytesConsumed == this._buffer.Length) this._isFinished = true;

        // inform stream buffer read state
        return !this._isFinished;
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
        while (this.TokenType != JsonTokenType.StartObject && this.TokenType != JsonTokenType.StartArray && !cancellationToken.IsCancellationRequested)
            await this.ReadAsync(cancellationToken).ConfigureAwait(false);

        // Temp storage for joined chunk data. Note: length required is unknown
        using MemoryStream stream = new MemoryStream();
        this._writer = PipeWriter.Create(stream, new(leaveOpen: true));

        // fill temp stream with json object
        if (!await this.GetJsonObjectAsync(stream, cancellationToken).ConfigureAwait(false))
            return default;

        // deserialize object from temp stream
        TValue? result = await JsonSerializer
            .DeserializeAsync<TValue>(stream, options, cancellationToken)
            .ConfigureAwait(false);

        // we are done buffering
        this._isBuffering = false;

        // temp stream is released and object returned
        return result;
    }

    void IDisposable.Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        
        this._reader.Complete();
    }

    #region Internal Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool JsonReader(bool isFinalBlock)
    {
        // reference the current position in the buffer
        ReadOnlySequence<byte> bytes = this._buffer.Slice(this._bytesConsumed);

        // read the next json token
        Utf8JsonReader reader = new Utf8JsonReader(bytes, isFinalBlock, this._jsonReaderState);

        bool result = reader.Read();

        this._bytesConsumed += (int)reader.BytesConsumed;   // within buffer window
        this.BytesConsumed += this._bytesConsumed;          // within stream

        this._jsonReaderState = reader.CurrentState;

        // nothing to read
        if (!result)
            return false;

        // store token
        this.TokenType = reader.TokenType;

        // store raw value
        ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
        this.RawValueBytes = new byte[span.Length];
        span.CopyTo(this.RawValueBytes);

        // success
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async ValueTask<bool> GetJsonObjectAsync(MemoryStream stream, CancellationToken cancellationToken)
    {
        // loop through data until end of object is found
        int depth = 0;

        // we are buffering all reads on the buffer stream
        this._isBuffering = true;
        this._bufferingStartIndex = this._bytesConsumed - 1;

        // walk the json object tree until we have the complete json object
        while (!cancellationToken.IsCancellationRequested)
        {
            if (this.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                depth++;
            else if (this.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                depth--;

            if (depth == 0)
                break;

            await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        // overflow
        this.WriteToBufferStream();

        // flush all writes
        await this._writer!.CompleteAsync().ConfigureAwait(false);

        // operation cancelled remotely
        if (cancellationToken.IsCancellationRequested)
            return false;

        // Point to beginning of the memory stream
        stream.Seek(0, SeekOrigin.Begin);

        // success
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteToBufferStream()
    {
        // Note: unable to find a better optimization for pushing ReadOnlySequence into a Stream
        int bytes = this._bytesConsumed - this._bufferingStartIndex;

        // store
        this._buffer.Slice(this._bufferingStartIndex, bytes).CopyTo(this._writer!.GetSpan(bytes));

        // manually advance buffer pointer
        this._writer.Advance(bytes);

        // slower
        //this._writer!.Write(this._buffer.Slice(this._bufferingStartIndex, this._bytesConsumed - this._bufferingStartIndex).ToArray());
    }

    #endregion

    #endregion
}