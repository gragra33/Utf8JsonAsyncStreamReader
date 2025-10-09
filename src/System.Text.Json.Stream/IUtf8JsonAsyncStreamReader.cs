namespace System.Text.Json.Stream;

/// <summary>
/// A high-performance API for forward-only, read-only stream access to the UTF-8 encoded JSON text with conditional deserialization of json objects.
/// </summary>
public interface IUtf8JsonAsyncStreamReader : IDisposable
{
    #region Properties

    /// <summary>
    /// The number of bytes read from the stream.
    /// </summary>
    int BytesConsumed { get; }

    /// <summary>
    /// The last processed JSON token in the UTF-8 encoded JSON text.
    /// </summary>
    JsonTokenType TokenType { get; }
    
    /// <summary>
    /// The last processed value in the UTF-8 encoded JSON text.
    /// </summary>
    object? Value { get; }

    #endregion
    
    #region Methods

    /// <summary>Asynchronously reads a sequence of bytes.</summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see langword="default" />.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.ValueTask`1" /> representing the asynchronous read operation state. <see langword="false" /> if finished, <see langword="true" /> if there is more to read.</returns>
    /// <exception cref="JsonStreamException">
    /// Thrown when the JSON stream is invalid, a token is incomplete, or the buffer is undersized for the current JSON structure.
    /// </exception>
    ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TResult"/>.
        /// The Stream will be read to end of current branch.
        /// </summary>
        /// <typeparam name="TResult">The type to deserialize the JSON value into.</typeparamref>
        /// <returns>A <typeparamref name="TResult"/> representation of the JSON value.</returns>
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
    /// <typeparamref name="TResult"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="TResult"/> or its serializable members.
        /// </exception>
    ValueTask<TResult?> DeserializeAsync<TResult>(JsonSerializerOptions? options = null, CancellationToken cancellationToken = default);

    #endregion
}