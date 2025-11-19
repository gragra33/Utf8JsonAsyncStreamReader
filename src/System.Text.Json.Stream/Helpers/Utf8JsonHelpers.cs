using System.Buffers.Text;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Stream;

/// <summary>
/// Provides extension methods for <see cref="Utf8JsonAsyncStreamReader"/> to extract strongly-typed values from JSON tokens.
/// </summary>
public static class Utf8JsonHelpers
{
    /// <summary>
    /// Get the value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetValue(this Utf8JsonAsyncStreamReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.PropertyName or JsonTokenType.String or JsonTokenType.Comment => GetString(reader),
            JsonTokenType.Number => GetNumber(reader),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => null,
        };
    }

    /// <summary>
    /// Get the numeric value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>numeric value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetNumber(Utf8JsonAsyncStreamReader reader)
    {
        if (reader.TryGetInt16(out short? shortValue))
            return shortValue;

        if (reader.TryGetInt32(out int? intValue))
            return intValue;

        if (reader.TryGetInt64(out long? longValue))
            return longValue;

        if (reader.TryGetDouble(out double? doubleValue))
            return doubleValue;

        return null;
    }

    /// <summary>
    /// Get the boolean value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Boolean value of <see cref="JsonTokenType"/>, or <see langword="null"/> if the token is not a boolean.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? GetBoolean(this Utf8JsonAsyncStreamReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => null
        };
    }

    /// <summary>
    /// Get the string value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>String value of <see cref="JsonTokenType"/>, or <see langword="null"/> if the token is <see cref="JsonTokenType.Null"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetString(this Utf8JsonAsyncStreamReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return reader.RawValueBytes is null ? null :
            // Use _valueLength to get the correct substring, not the full array length
            Encoding.UTF8.GetString(reader.RawValueBytes, 0, reader._valueLength);
    }

    /// <summary>
    /// Copies the current JSON token value from the source, unescaped as a UTF-8 string to the destination buffer.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="utf8Destination">A buffer to write the unescaped UTF-8 bytes into.</param>
    /// <returns>The number of bytes written to <paramref name="utf8Destination"/>.</returns>
    /// <remarks>
    /// This method will throw <see cref="ArgumentException"/> if the destination buffer is too small to hold the unescaped value.
    /// An appropriately sized buffer can be determined by consulting the length of <see cref="Utf8JsonAsyncStreamReader.RawValueBytes"/>,
    /// since the unescaped result is always less than or equal to the length of the encoded strings.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CopyString(this Utf8JsonAsyncStreamReader reader, Span<byte> utf8Destination)
    {
        if (reader.RawValueBytes is null)
        {
            return 0;
        }

        ReadOnlySpan<byte> source = new(reader.RawValueBytes, 0, reader._valueLength);
        
        if (source.Length > utf8Destination.Length)
        {
            throw new ArgumentException("The destination buffer is too small to hold the unescaped value.");
        }

        source.CopyTo(utf8Destination);
        return source.Length;
    }

    /// <summary>
    /// Copies the current JSON token value from the source, unescaped, and transcoded as a UTF-16 char buffer.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="destination">A buffer to write the transcoded UTF-16 characters into.</param>
    /// <returns>The number of characters written to <paramref name="destination"/>.</returns>
    /// <remarks>
    /// This method will throw <see cref="ArgumentException"/> if the destination buffer is too small to hold the unescaped value.
    /// An appropriately sized buffer can be determined by consulting the length of <see cref="Utf8JsonAsyncStreamReader.RawValueBytes"/>,
    /// since the unescaped result is always less than or equal to the length of the encoded strings.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CopyString(this Utf8JsonAsyncStreamReader reader, Span<char> destination)
    {
        string? str = reader.GetString();
        
        if (str is null)
        {
            return 0;
        }

        if (str.Length > destination.Length)
        {
            throw new ArgumentException("The destination buffer is too small to hold the unescaped value.");
        }

        str.AsSpan().CopyTo(destination);
        return str.Length;
    }

    /// <summary>
    /// Get the byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Byte value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetByte(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetByte(out byte? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetByte(this Utf8JsonAsyncStreamReader reader, out byte? value)
    {
        if (reader.RawValueBytes != null  && TryGetByteCore(out byte tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing byte values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetByteCore(out byte value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out byte tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the unsigned byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned byte value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetUByte(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUByte(out uint? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the unsigned byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetUByte(this Utf8JsonAsyncStreamReader reader, out uint? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetUByteCore(out uint tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing unsigned byte values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetUByteCore(out uint value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out uint tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the signed byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Signed byte value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte GetSByte(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetSByte(out sbyte? value) ? value ?? 0 : default;

    /// <summary>
    /// Try getting the signed byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSByte(this Utf8JsonAsyncStreamReader reader, out sbyte? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetSByteCore(out sbyte tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing signed byte values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetSByteCore(out sbyte value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out sbyte tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>16-bit integer value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short GetInt16(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetInt16(out short? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetInt16(this Utf8JsonAsyncStreamReader reader, out short? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetInt16Core(out short tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing 16-bit integer values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetInt16Core(out short value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out short tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the unsigned 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned 16-bit integer value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetUInt16(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUInt16(out uint? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the unsigned 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetUInt16(this Utf8JsonAsyncStreamReader reader, out uint? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetUInt16Core(out uint tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing unsigned 16-bit integer values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetUInt16Core(out uint value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out ushort tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>32-bit integer value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetInt32(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetInt32(out int? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetInt32(this Utf8JsonAsyncStreamReader reader, out int? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetInt32Core(out int tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing 32-bit integer values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetInt32Core(out int value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out int tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the unsigned 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned 32-bit integer value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetUInt32(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUInt32(out uint? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the unsigned 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetUInt32(this Utf8JsonAsyncStreamReader reader, out uint? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetUInt32Core(out uint tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing unsigned 32-bit integer values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetUInt32Core(out uint value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out uint tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the 64-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>64-bit integer value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetInt64(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetInt64(out long? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the 64-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetInt64(this Utf8JsonAsyncStreamReader reader, out long? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetInt64Core(out long tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing 64-bit integer values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetInt64Core(out long value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out long tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the unsigned 64-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned 64-bit integer value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetUInt64(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUInt64(out ulong? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the 64-bit unsigned integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetUInt64(this Utf8JsonAsyncStreamReader reader, out ulong? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetUInt64Core(out ulong tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing unsigned 64-bit integer values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetUInt64Core(out ulong value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out ulong tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the single-precision floating-point number from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Single-precision floating-point number value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetSingle(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetSingle(out float? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the single-precision floating-point number value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSingle(this Utf8JsonAsyncStreamReader reader, out float? value)
    {
        if (reader.RawValueBytes != null 
            && Utf8Parser.TryParse(new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength), out float tmp, out int bytesConsumed)
            && reader._valueLength == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the double-precision floating-point number from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Double-precision floating-point number value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetDouble(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDouble(out double? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the double-precision floating-point number from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetDouble(this Utf8JsonAsyncStreamReader reader, out double? value)
    {
        if (reader.RawValueBytes != null 
            && Utf8Parser.TryParse(new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength), out double tmp, out int bytesConsumed)
            && reader._valueLength == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the <see cref="T:System.Decimal" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns><see cref="T:System.Decimal" /> value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Decimal GetDecimal(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDecimal(out decimal? value) ? value ?? 0 : 0;

    /// <summary>
    /// Try getting the <see cref="T:System.Decimal" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetDecimal(this Utf8JsonAsyncStreamReader reader, out decimal? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetDecimalCore(out decimal tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing decimal values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetDecimalCore(out decimal value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out decimal tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Get the <see cref="T:System.DateTime" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns><see cref="T:System.DateTime" /> value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetDateTime(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDateTime(out DateTime? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the <see cref="T:System.DateTime" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetDateTime(this Utf8JsonAsyncStreamReader reader, out DateTime? value)
    {
        string? str = reader.GetString();
        if (str != null && DateTime.TryParse(str, out DateTime tmp))
        {
            value = tmp;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Get the <see cref="T:System.DateTimeOffset" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>A <see cref="T:System.DateTimeOffset" /> value parsed from the JSON token.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset GetDateTimeOffset(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDateTimeOffset(out DateTimeOffset? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the <see cref="T:System.DateTimeOffset" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetDateTimeOffset(this Utf8JsonAsyncStreamReader reader, out DateTimeOffset? value)
    {
        string? str = reader.GetString();
        if (str != null && DateTimeOffset.TryParse(str, out DateTimeOffset tmp))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Parses the current JSON token value from the source and decodes the Base64 encoded JSON string as bytes.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>The decoded bytes from the Base64 encoded string.</returns>
    /// <exception cref="FormatException">
    /// The JSON string contains data outside the expected Base64 range, or if it contains invalid/more than two padding characters,
    /// or is incomplete (i.e. the JSON string length is not a multiple of 4).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[]? GetBytesFromBase64(this Utf8JsonAsyncStreamReader reader)
    {
        if (!reader.TryGetBytesFromBase64(out byte[]? value))
        {
            throw new FormatException("The input is not a valid Base64 string.");
        }
        return value;
    }

    /// <summary>
    /// Tries to parse the current JSON token value as a Base64 encoded string and decode it to bytes.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the decoded bytes if successful.</param>
    /// <returns><see langword="true" /> if the value was successfully decoded; <see langword="false" /> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetBytesFromBase64(this Utf8JsonAsyncStreamReader reader, out byte[]? value)
    {
        if (reader.RawValueBytes is null)
        {
            value = null;
            return false;
        }

        try
        {
            string? str = reader.GetString();
            if (str is null)
            {
                value = null;
                return false;
            }

            value = Convert.FromBase64String(str);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Get the <see cref="T:System.Guid" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns><see cref="T:System.Guid" /> value of <see cref="JsonTokenType"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid GetGuid(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetGuid(out Guid? value) ? value ?? Guid.Empty : Guid.Empty;

    /// <summary>
    /// Try getting the <see cref="T:System.Guid" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetGuid(this Utf8JsonAsyncStreamReader reader, out Guid? value)
    {
        if (reader.RawValueBytes != null 
            && TryGetGuidCore(out Guid tmp, new ReadOnlySpan<byte>(reader.RawValueBytes, 0, reader._valueLength)))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Core method for parsing Guid values from spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetGuidCore(out Guid value, ReadOnlySpan<byte> span)
    {
        if (Utf8Parser.TryParse(span, out Guid tmp, out int bytesConsumed)
            && span.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = Guid.Empty;
        return false;
    }
}