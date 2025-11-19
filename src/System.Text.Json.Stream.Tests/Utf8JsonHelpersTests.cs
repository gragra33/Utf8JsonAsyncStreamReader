using System.Text.Json;
using System.Text.Json.Stream;
using FluentAssertions;
using Xunit;
using JsonReader = System.Text.Json.Stream.Utf8JsonAsyncStreamReader;

namespace UnitTests.Tests.Readers.System.Text.Json;

/// <summary>
/// Comprehensive tests for the <see cref="System.Text.Json.Stream.Utf8JsonHelpers"/> extension methods.
/// </summary>
[Collection("Sequential")]
public class Utf8JsonHelpersTests
{
    private static readonly DateTime testDateTime = new(2022, 09, 01, 11, 12, 13);
    private static readonly DateTimeOffset testDateTimeOffset = new(testDateTime, new TimeSpan(0, 30, 0));
    private static readonly Guid testGuid = Guid.NewGuid();

    #region String Methods

    [Fact]
    async Task GetString_WithValidString_ReturnsString()
    {
        // Arrange
        var json = @"{""Name"": ""John""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Name"
        await reader.ReadAsync(); // "John"

        // Assert
        reader.GetString().Should().Be("John");
    }

    [Fact]
    async Task GetString_WithNullValue_ReturnsNull()
    {
        // Arrange
        var json = @"{""Name"": null}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Name"
        await reader.ReadAsync(); // null

        // Assert
        reader.GetString().Should().BeNull();
    }

    [Fact]
    async Task CopyString_WithByteSpan_CopiesCorrectly()
    {
        // Arrange
        var json = @"{""Value"": ""Hello""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);
        byte[] buffer = new byte[20];

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // "Hello"
        int bytesWritten = reader.CopyString(new Span<byte>(buffer));

        // Assert
        bytesWritten.Should().Be(5);
        Encoding.UTF8.GetString(buffer, 0, bytesWritten).Should().Be("Hello");
    }

    [Fact]
    async Task CopyString_WithCharSpan_CopiesCorrectly()
    {
        // Arrange
        var json = @"{""Value"": ""World""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);
        char[] buffer = new char[20];

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // "World"
        int charsWritten = reader.CopyString(new Span<char>(buffer));

        // Assert
        charsWritten.Should().Be(5);
        new string(buffer, 0, charsWritten).Should().Be("World");
    }

    [Fact]
    async Task CopyString_WithByteSpan_ThrowsWhenBufferTooSmall()
    {
        // Arrange
        var json = @"{""Value"": ""HelloWorld""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);
        byte[] buffer = new byte[2]; // Too small

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // "HelloWorld"

        // Assert
        var action = () => reader.CopyString(new Span<byte>(buffer));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    async Task CopyString_WithCharSpan_ThrowsWhenBufferTooSmall()
    {
        // Arrange
        var json = @"{""Value"": ""HelloWorld""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);
        char[] buffer = new char[2]; // Too small

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // "HelloWorld"

        // Assert
        var action = () => reader.CopyString(new Span<char>(buffer));
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Boolean Methods

    [Fact]
    async Task GetBoolean_WithTrue_ReturnsTrue()
    {
        // Arrange
        var json = @"{""Active"": true}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Active"
        await reader.ReadAsync(); // true

        // Assert
        reader.GetBoolean().Should().Be(true);
    }

    [Fact]
    async Task GetBoolean_WithFalse_ReturnsFalse()
    {
        // Arrange
        var json = @"{""Active"": false}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Active"
        await reader.ReadAsync(); // false

        // Assert
        reader.GetBoolean().Should().Be(false);
    }

    #endregion

    #region Byte Methods

    [Fact]
    async Task GetByte_WithValidByte_ReturnsByte()
    {
        // Arrange
        var json = @"{""Value"": 255}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 255

        // Assert
        reader.GetByte().Should().Be(255);
    }

    [Fact]
    async Task TryGetByte_WithValidByte_ReturnsTrue()
    {
        // Arrange
        var json = @"{""Value"": 128}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 128
        bool result = reader.TryGetByte(out byte? value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(128);
    }

    [Fact]
    async Task GetUByte_WithValidValue_ReturnsUByte()
    {
        // Arrange
        var json = @"{""Value"": 200}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 200

        // Assert
        reader.GetUByte().Should().Be(200);
    }

    [Fact]
    async Task TryGetUByte_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var json = @"{""Value"": 100}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 100
        bool result = reader.TryGetUByte(out uint? value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(100);
    }

    [Fact]
    async Task GetSByte_WithValidValue_ReturnsSByte()
    {
        // Arrange
        var json = @"{""Value"": -50}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // -50

        // Assert
        reader.GetSByte().Should().Be(-50);
    }

    [Fact]
    async Task TryGetSByte_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var json = @"{""Value"": 75}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 75
        bool result = reader.TryGetSByte(out sbyte? value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(75);
    }

    #endregion

    #region Integer Methods (Int16, Int32, Int64, UInt16, UInt32, UInt64)

    [Fact]
    async Task GetInt16_WithValidValue_ReturnsInt16()
    {
        // Arrange
        var json = @"{""Value"": 32000}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 32000

        // Assert
        reader.GetInt16().Should().Be(32000);
    }

    [Fact]
    async Task GetInt32_WithValidValue_ReturnsInt32()
    {
        // Arrange
        var json = @"{""Value"": 2147483647}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 2147483647

        // Assert
        reader.GetInt32().Should().Be(2147483647);
    }

    [Fact]
    async Task GetInt64_WithValidValue_ReturnsInt64()
    {
        // Arrange
        var json = @"{""Value"": 9223372036854775807}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 9223372036854775807

        // Assert
        reader.GetInt64().Should().Be(9223372036854775807);
    }

    [Fact]
    async Task GetUInt16_WithValidValue_ReturnsUInt16()
    {
        // Arrange
        var json = @"{""Value"": 65000}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 65000

        // Assert
        reader.GetUInt16().Should().Be(65000);
    }

    [Fact]
    async Task GetUInt32_WithValidValue_ReturnsUInt32()
    {
        // Arrange
        var json = @"{""Value"": 4294967295}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 4294967295

        // Assert
        reader.GetUInt32().Should().Be(4294967295);
    }

    [Fact]
    async Task GetUInt64_WithValidValue_ReturnsUInt64()
    {
        // Arrange
        var json = @"{""Value"": 18446744073709551615}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 18446744073709551615

        // Assert
        reader.GetUInt64().Should().Be(18446744073709551615);
    }

    #endregion

    #region Floating Point Methods

    [Fact]
    async Task GetSingle_WithValidValue_ReturnsSingle()
    {
        // Arrange
        var json = @"{""Value"": 123.45}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 123.45

        // Assert
        reader.GetSingle().Should().BeApproximately(123.45f, 0.01f);
    }

    [Fact]
    async Task GetDouble_WithValidValue_ReturnsDouble()
    {
        // Arrange
        var json = @"{""Value"": 123456.789}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 123456.789

        // Assert
        reader.GetDouble().Should().BeApproximately(123456.789, 0.01);
    }

    #endregion

    #region Decimal Methods

    [Fact]
    async Task GetDecimal_WithValidValue_ReturnsDecimal()
    {
        // Arrange
        var json = @"{""Value"": 12345.67}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // 12345.67

        // Assert
        reader.GetDecimal().Should().Be(12345.67m);
    }

    [Fact]
    async Task TryGetDecimal_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var json = @"{""Value"": -98765.43}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // -98765.43
        bool result = reader.TryGetDecimal(out decimal? value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(-98765.43m);
    }

    #endregion

    #region DateTime Methods

    [Fact]
    async Task GetDateTime_WithValidValue_ReturnsDateTime()
    {
        // Arrange
        var json = $@"{{""Value"": ""{testDateTime:O}""}}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // datetime

        // Assert
        reader.GetDateTime().Should().Be(testDateTime);
    }

    [Fact]
    async Task GetDateTimeOffset_WithValidValue_ReturnsDateTimeOffset()
    {
        // Arrange
        var json = $@"{{""Value"": ""{testDateTimeOffset:O}""}}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // datetimeoffset

        // Assert
        reader.GetDateTimeOffset().Should().Be(testDateTimeOffset);
    }

    #endregion

    #region GUID Methods

    [Fact]
    async Task GetGuid_WithValidValue_ReturnsGuid()
    {
        // Arrange
        var json = $@"{{""Value"": ""{testGuid:D}""}}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // guid

        // Assert
        reader.GetGuid().Should().Be(testGuid);
    }

    [Fact]
    async Task TryGetGuid_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var json = $@"{{""Value"": ""{testGuid:D}""}}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // guid
        bool result = reader.TryGetGuid(out Guid? value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(testGuid);
    }

    #endregion

    #region Base64 Methods

    [Fact]
    async Task GetBytesFromBase64_WithValidBase64_ReturnsBytes()
    {
        // Arrange
        var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello"));
        var json = $@"{{""Value"": ""{base64String}""}}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // base64 string

        // Assert
        byte[]? result = reader.GetBytesFromBase64();
        Encoding.UTF8.GetString(result!).Should().Be("Hello");
    }

    [Fact]
    async Task GetBytesFromBase64_WithInvalidBase64_ThrowsFormatException()
    {
        // Arrange
        var json = @"{""Value"": ""!!!INVALID!!!""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // invalid base64

        // Assert
        var action = () => reader.GetBytesFromBase64();
        action.Should().Throw<FormatException>();
    }

    [Fact]
    async Task TryGetBytesFromBase64_WithValidBase64_ReturnsTrue()
    {
        // Arrange
        var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes("World"));
        var json = $@"{{""Value"": ""{base64String}""}}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // base64 string
        bool result = reader.TryGetBytesFromBase64(out byte[]? value);

        // Assert
        result.Should().BeTrue();
        Encoding.UTF8.GetString(value!).Should().Be("World");
    }

    [Fact]
    async Task TryGetBytesFromBase64_WithInvalidBase64_ReturnsFalse()
    {
        // Arrange
        var json = @"{""Value"": ""@@@NOTBASE64@@@""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Value"
        await reader.ReadAsync(); // invalid base64
        bool result = reader.TryGetBytesFromBase64(out byte[]? value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    #endregion

    #region GetValue Methods

    [Fact]
    async Task GetValue_WithString_ReturnsString()
    {
        // Arrange
        var json = @"{""Name"": ""Test""}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Name"
        await reader.ReadAsync(); // "Test"

        // Assert
        reader.GetValue().Should().Be("Test");
    }

    [Fact]
    async Task GetValue_WithNumber_ReturnsNumber()
    {
        // Arrange
        var json = @"{""Count"": 42}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Count"
        await reader.ReadAsync(); // 42

        // Assert
        reader.GetValue().Should().Be(42);
    }

    [Fact]
    async Task GetValue_WithBoolean_ReturnsBoolean()
    {
        // Arrange
        var json = @"{""Enabled"": true}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new JsonReader(stream);

        // Act
        await reader.ReadAsync(); // {
        await reader.ReadAsync(); // "Enabled"
        await reader.ReadAsync(); // true

        // Assert
        reader.GetValue().Should().Be(true);
    }

    #endregion
}
