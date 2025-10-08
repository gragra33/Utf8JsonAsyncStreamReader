using System.Text.Json;
using System.Text.Json.Stream;
using FluentAssertions.Common;
using UnitTests.Tests.Readers.System.Text.Json.Models;
using JsonReader = System.Text.Json.Stream.Utf8JsonAsyncStreamReader;

namespace UnitTests.Tests.Readers.System.Text.Json;

/// <summary>
/// Comprehensive tests for the <see cref="System.Text.Json.Stream.Utf8JsonAsyncStreamReader"/> class functionality.
/// </summary>
[Collection("Sequential")]
public class Utf8JsonAsyncStreamReader
{
    /// <summary>
    /// Test DateTime value used across multiple tests.
    /// </summary>
    private static readonly DateTime testDateTime = new(2022, 09, 01, 11, 12, 13);
    
    /// <summary>
    /// Test DateTimeOffset value used across multiple tests.
    /// </summary>
    private static readonly DateTimeOffset testDateTimeOffset = testDateTime.ToDateTimeOffset(new TimeSpan(0, 30, 0));
    
    /// <summary>
    /// Test Guid value used across multiple tests.
    /// </summary>
    private static readonly Guid testGuid = Guid.NewGuid();

    /// <summary>
    /// JSON string containing various property types for testing.
    /// </summary>
    private readonly string jsonProperties = JsonSerializer.Serialize
    (
        new
        {
            IntPositive = int.MaxValue,
            IntNegative = int.MinValue,
            LongPositive = long.MaxValue,
            LongNegative = long.MinValue,
            ShortPositive = short.MaxValue,
            ShortNegative = short.MinValue,
            FloatPositive = float.MaxValue,
            FloatNegative = float.MinValue,
            DoublePositive = double.MaxValue,
            DoubleNegative = double.MinValue,
            BoolValue = true,
            StringValue = "this is a string",
            DateTimeValue = testDateTime,
            DateTimeOffsetValue = testDateTimeOffset,
            GuidValue = testGuid
        }
    );

    /// <summary>
    /// JSON string containing various property types using camelCase naming for testing.
    /// </summary>
    private readonly string jsonPropertiesCamelCase = JsonSerializer.Serialize
    (
        new
        {
            IntPositive = int.MaxValue,
            IntNegative = int.MinValue,
            LongPositive = long.MaxValue,
            LongNegative = long.MinValue,
            ShortPositive = short.MaxValue,
            ShortNegative = short.MinValue,
            FloatPositive = float.MaxValue,
            FloatNegative = float.MinValue,
            DoublePositive = double.MaxValue,
            DoubleNegative = double.MinValue,
            BoolValue = true,
            StringValue = "this is a string",
            DateTimeValue = testDateTime,
            DateTimeOffsetValue = testDateTimeOffset,
            GuidValue = testGuid
        }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
    );

    /// <summary>
    /// JSON string containing an object with a collection property for testing.
    /// </summary>
    private readonly string jsonCollection = JsonSerializer.Serialize
    (
        new
        {
            IntPositive = int.MaxValue,
            ListItems = new List<string>
            {
                "item 1",
                "item 2",
                "item 3"
            }
        }
    );

    /// <summary>
    /// Tests reading JSON properties sequentially and verifying token types and values.
    /// </summary>
    [Fact]
    async Task Json_Property()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(jsonProperties));
        JsonReader reader = new(stream); // System.Text.Json.Utf8JsonAsyncStreamReader

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.StartObject);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("IntPositive");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        ((int)reader.Value!).Should().Be(int.MaxValue);
        reader.GetInt32().Should().Be(int.MaxValue);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("IntNegative");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        ((int)reader.Value!).Should().Be(int.MinValue);
        reader.GetInt32().Should().Be(int.MinValue);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("LongPositive");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        ((long)reader.Value!).Should().Be(long.MaxValue);
        reader.GetInt64().Should().Be(long.MaxValue);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("LongNegative");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        ((long)reader.Value!).Should().Be(long.MinValue);
        reader.GetInt64().Should().Be(long.MinValue);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("ShortPositive");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        ((short)reader.Value!).Should().Be(short.MaxValue);
        reader.GetInt16().Should().Be(short.MaxValue);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("ShortNegative");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        ((short)reader.Value!).Should().Be(short.MinValue);
        reader.GetInt16().Should().Be(short.MinValue);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("FloatPositive");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        float.Parse(reader.Value.ToString()!).Should().BeApproximately(float.MaxValue, 0.01F);
        reader.GetSingle().Should().BeApproximately(float.MaxValue, 0.01F);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("FloatNegative");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        float.Parse(reader.Value.ToString()!).Should().BeApproximately(float.MinValue, 0.01F);
        reader.GetSingle().Should().BeApproximately(float.MinValue, 0.01F);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("DoublePositive");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        double.Parse(reader.Value.ToString()!).Should().BeApproximately(double.MaxValue, 0.01D);
        reader.GetDouble().Should().BeApproximately(double.MaxValue, 0.01D);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("DoubleNegative");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        double.Parse(reader.Value.ToString()!).Should().BeApproximately(double.MinValue, 0.01D);
        reader.GetDouble().Should().BeApproximately(double.MinValue, 0.01D);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("BoolValue");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.True);
        ((bool)reader.Value!).Should().Be(true);
        reader.GetBoolean().Should().Be(true);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("StringValue");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.String);
        reader.Value!.Should().Be("this is a string");
        reader.GetString().Should().Be("this is a string");

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("DateTimeValue");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.String);
        DateTime.Parse(reader.Value.ToString()!).Should().Be(testDateTime);
        reader.GetDateTime().Should().Be(testDateTime);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("DateTimeOffsetValue");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.String);
        DateTimeOffset.Parse(reader.GetString()!).Should().Be(testDateTimeOffset);
        reader.GetDateTimeOffset().Should().Be(testDateTimeOffset);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("GuidValue");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.String);
        reader.GetString().Should().Be(testGuid.ToString());
        reader.Value!.Should().Be(testGuid.ToString());
        reader.GetGuid().Should().Be(testGuid);

        bool readResult = await reader.ReadAsync();
        readResult.Should().BeFalse(); // end of the json graph?
        reader.TokenType.Should().Be(JsonTokenType.EndObject);

        // trying to read past the end of the json graph
        readResult = await reader.ReadAsync();
        readResult.Should().BeFalse();
        reader.TokenType.Should().Be(JsonTokenType.None);
    }

    /// <summary>
    /// Tests reading JSON collections (arrays) and verifying token types and values.
    /// </summary>
    [Fact]
    async Task Json_Collection()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(jsonCollection));
        JsonReader reader = new(stream); // System.Text.Json.Utf8JsonAsyncStreamReader

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.StartObject);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("IntPositive");
        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.Number);
        ((int)reader.Value!).Should().Be(int.MaxValue);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.PropertyName);
        reader.Value.Should().Be("ListItems");

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.StartArray);

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.String);
        reader.Value!.Should().Be("item 1");

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.String);
        reader.Value!.Should().Be("item 2");

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.String);
        reader.Value!.Should().Be("item 3");

        await reader.ReadAsync();
        reader.TokenType.Should().Be(JsonTokenType.EndArray);

        bool readResult2 = await reader.ReadAsync();
        readResult2.Should().BeFalse(); // end of the json graph?
        reader.TokenType.Should().Be(JsonTokenType.EndObject);

        // trying to read past the end of the json graph
        bool readResult3 = await reader.ReadAsync();
        readResult3.Should().BeFalse();
        reader.TokenType.Should().Be(JsonTokenType.None);
    }

    /// <summary>
    /// Tests deserializing JSON into a strongly-typed object with various property types.
    /// </summary>
    [Fact]
    async Task Deserialize_Property()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(jsonProperties));
        JsonReader reader = new(stream); // System.Text.Json.Utf8JsonAsyncStreamReader

        JsonPropertyObject? result = await reader.DeserializeAsync<JsonPropertyObject>();

        result!.IntPositive.Should().Be(int.MaxValue);
        result.IntNegative.Should().Be(int.MinValue);
        result.LongPositive.Should().Be(long.MaxValue);
        result.LongNegative.Should().Be(long.MinValue);
        result.ShortPositive.Should().Be(short.MaxValue);
        result.ShortNegative.Should().Be(short.MinValue);
        result.FloatPositive.Should().BeApproximately(float.MaxValue, 0.01F);
        result.FloatNegative.Should().BeApproximately(float.MinValue, 0.01F);
        result.DoublePositive.Should().BeApproximately(double.MaxValue, 0.01F);
        result.DoubleNegative.Should().BeApproximately(double.MinValue, 0.01F);
        result.BoolValue.Should().Be(true);
        result.StringValue.Should().Be("this is a string");
        result.DateTimeValue.Should().Be(new DateTime(2022, 09, 01, 11, 12, 13));
    }

    /// <summary>
    /// Tests deserializing JSON with camelCase property naming into a strongly-typed object.
    /// </summary>
    [Fact]
    async Task Deserialize_Property_CamelCase()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(jsonPropertiesCamelCase));
        JsonReader reader = new(stream); // System.Text.Json.Utf8JsonAsyncStreamReader

        JsonPropertyObject? result = await reader.DeserializeAsync<JsonPropertyObject>(new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result!.IntPositive.Should().Be(int.MaxValue);
        result.IntNegative.Should().Be(int.MinValue);
        result.LongPositive.Should().Be(long.MaxValue);
        result.LongNegative.Should().Be(long.MinValue);
        result.ShortPositive.Should().Be(short.MaxValue);
        result.ShortNegative.Should().Be(short.MinValue);
        result.FloatPositive.Should().BeApproximately(float.MaxValue, 0.01F);
        result.FloatNegative.Should().BeApproximately(float.MinValue, 0.01F);
        result.DoublePositive.Should().BeApproximately(double.MaxValue, 0.01F);
        result.DoubleNegative.Should().BeApproximately(double.MinValue, 0.01F);
        result.BoolValue.Should().Be(true);
        result.StringValue.Should().Be("this is a string");
        result.DateTimeValue.Should().Be(new DateTime(2022, 09, 01, 11, 12, 13));
    }

    /// <summary>
    /// Tests deserializing JSON containing collections into a strongly-typed object.
    /// </summary>
    [Fact]
    async Task Deserialize_Collection()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(jsonCollection));
        JsonReader reader = new(stream); // System.Text.Json.Utf8JsonAsyncStreamReader

        JsonCollectionObject? result = await reader.DeserializeAsync<JsonCollectionObject>();

        result!.IntPositive.Should().Be(int.MaxValue);

        result.ListItems!.Count.Should().Be(3);
        result.ListItems[0].Should().Be("item 1");
        result.ListItems[1].Should().Be("item 2");
        result.ListItems[2].Should().Be("item 3");
    }
}