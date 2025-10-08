namespace System.Text.Json.Stream.Old;

/// <summary>
/// Wrapper for the old Utf8JsonAsyncStreamReader v1.2.0 from NuGet
/// to avoid namespace collision with the new version.
/// </summary>
public sealed class Utf8JsonAsyncStreamReaderOld(IO.Stream stream, int minimumBufferSize = -1, bool leaveOpen = false)
    : IDisposable
{
    private readonly Utf8JsonAsyncStreamReader _reader = new(stream, minimumBufferSize, leaveOpen);

    public int BytesConsumed => _reader.BytesConsumed;
    public JsonTokenType TokenType => _reader.TokenType;
    public object? Value => _reader.Value;

    public ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
        => _reader.ReadAsync(cancellationToken);

    public ValueTask<TValue?> DeserializeAsync<TValue>(JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        => _reader.DeserializeAsync<TValue>(options, cancellationToken);

    public void Dispose()
    {
        ((IDisposable)_reader).Dispose();
    }
}
