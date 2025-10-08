using System.Buffers.Text;

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
    public static bool? GetBoolean(this Utf8JsonAsyncStreamReader reader)
        => reader.TokenType == JsonTokenType.True ? true : reader.TokenType == JsonTokenType.False ? false : null;

    /// <summary>
    /// Get the string value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>String value of <see cref="JsonTokenType"/></returns>
    public static string? GetString(this Utf8JsonAsyncStreamReader reader)
        => reader.RawValueBytes is null ? null : Encoding.UTF8.GetString(reader.RawValueBytes);

    /// <summary>
    /// Get the byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Byte value of <see cref="JsonTokenType"/></returns>
    public static byte GetByte(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetByte(out byte? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetByte(this Utf8JsonAsyncStreamReader reader, out byte? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out byte tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the unsigned byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned byte value of <see cref="JsonTokenType"/></returns>
    public static uint GetUByte(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUByte(out uint? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the unsigned byte value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetUByte(this Utf8JsonAsyncStreamReader reader, out uint? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out uint tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>16-bit integer value of <see cref="JsonTokenType"/></returns>
    public static short GetInt16(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetInt16(out short? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetInt16(this Utf8JsonAsyncStreamReader reader, out short? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out short tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the unsigned 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned 16-bit integer value of <see cref="JsonTokenType"/></returns>
    public static uint GetUInt16(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUInt16(out uint? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the unsigned 16-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetUInt16(this Utf8JsonAsyncStreamReader reader, out uint? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out uint tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>32-bit integer value of <see cref="JsonTokenType"/></returns>
    public static int GetInt32(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetInt32(out int? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetInt32(this Utf8JsonAsyncStreamReader reader, out int? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out int tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the unsigned 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned 32-bit integer value of <see cref="JsonTokenType"/></returns>
    public static uint GetUInt32(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUInt32(out uint? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the unsigned 32-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetUInt32(this Utf8JsonAsyncStreamReader reader, out uint? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out uint tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the 64-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>64-bit integer value of <see cref="JsonTokenType"/></returns>
    public static long GetInt64(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetInt64(out long? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the 64-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetInt64(this Utf8JsonAsyncStreamReader reader, out long? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out long tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the unsigned 64-bit integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Unsigned 64-bit integer value of <see cref="JsonTokenType"/></returns>
    public static ulong GetUInt64(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetUInt64(out ulong? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the 64-bit unsigned integer value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetUInt64(this Utf8JsonAsyncStreamReader reader, out ulong? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out ulong tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the single-precision floating-point number from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns>Single-precision floating-point number value of <see cref="JsonTokenType"/></returns>
    public static float GetSingle(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetSingle(out float? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the single-precision floating-point number value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetSingle(this Utf8JsonAsyncStreamReader reader, out float? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out float tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
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
    public static double GetDouble(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDouble(out double? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the double-precision floating-point number from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetDouble(this Utf8JsonAsyncStreamReader reader, out double? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out double tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
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
    public static Decimal GetDecimal(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDecimal(out decimal? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the <see cref="T:System.Decimal" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetDecimal(this Utf8JsonAsyncStreamReader reader, out decimal? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out decimal tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the <see cref="T:System.DateTime" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns><see cref="T:System.DateTime" /> value of <see cref="JsonTokenType"/></returns>
    public static DateTime GetDateTime(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDateTime(out DateTime? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the <see cref="T:System.DateTime" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetDateTime(this Utf8JsonAsyncStreamReader reader, out DateTime? value)
    {
        if (reader.RawValueBytes != null && DateTime.TryParse(reader.GetString(), out DateTime tmp))
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
    public static DateTimeOffset GetDateTimeOffset(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetDateTimeOffset(out DateTimeOffset? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the <see cref="T:System.DateTimeOffset" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetDateTimeOffset(this Utf8JsonAsyncStreamReader reader, out DateTimeOffset? value)
    {
        if (reader.RawValueBytes != null && DateTimeOffset.TryParse(reader.GetString(), out DateTimeOffset tmp))
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get the <see cref="T:System.Guid" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <returns><see cref="T:System.Guid" /> value of <see cref="JsonTokenType"/></returns>
    public static Guid GetGuid(this Utf8JsonAsyncStreamReader reader)
        => reader.TryGetGuid(out Guid? value) ? value ?? default : default;

    /// <summary>
    /// Try getting the <see cref="T:System.Guid" /> value from the reader.
    /// </summary>
    /// <param name="reader"><see cref="Utf8JsonAsyncStreamReader"/> instance.</param>
    /// <param name="value">When the method returns, contains the value parsed from the reader, if the parsing operation succeeded.</param>
    /// <returns><see langword="true" /> for success; <see langword="false" /> if the reader was not syntactically valid.</returns>
    public static bool TryGetGuid(this Utf8JsonAsyncStreamReader reader, out Guid? value)
    {
        if (reader.RawValueBytes != null && Utf8Parser.TryParse(reader.RawValueBytes, out Guid tmp, out int bytesConsumed)
                                         && reader.RawValueBytes.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = null;
        return false;
    }
}