using BenchmarkDotNet.Attributes;

namespace System.Text.Json.Stream.Benchmarks;

/// <summary>
/// Benchmark class for testing JSON parsing performance with different JSON sizes and approaches.
/// Compares deserialization performance and token-by-token reading against System.Text.Json baseline.
/// </summary>
[MemoryDiagnoser]
public class JsonParsingBenchmark
{
    /// <summary>
    /// Small JSON data for benchmarking (typical API response).
    /// Contains a simple object with basic properties.
    /// </summary>
    private byte[] _smallJson = null!;
    
    /// <summary>
    /// Medium JSON data for benchmarking (array of 100 user objects).
    /// Represents a typical paginated API response with moderate complexity.
    /// </summary>
    private byte[] _mediumJson = null!;
    
    /// <summary>
    /// Large JSON data for benchmarking (complex nested structure with 1000 objects).
    /// Contains nested objects, dictionaries, and arrays to test complex scenarios.
    /// </summary>
    private byte[] _largeJson = null!;
    
    /// <summary>
    /// Global setup method that initializes the test JSON data of different sizes.
    /// Creates small, medium, and large JSON datasets representing real-world scenarios
    /// from simple API responses to complex nested data structures.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // Small JSON (typical API response)
        var smallObject = new
        {
            Id = 123,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            Score = 95.5
        };
        _smallJson = JsonSerializer.SerializeToUtf8Bytes(smallObject);
        
        // Medium JSON (array of objects)
        var mediumObject = new
        {
            Users = Enumerable.Range(1, 100).Select(i => new
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                IsActive = i % 2 == 0,
                Score = Random.Shared.NextDouble() * 100
            }).ToArray()
        };
        _mediumJson = JsonSerializer.SerializeToUtf8Bytes(mediumObject);
        
        // Large JSON (complex nested structure)
        var largeObject = new
        {
            Metadata = new { Version = "1.0", Timestamp = DateTime.UtcNow },
            Data = Enumerable.Range(1, 1000).Select(i => new
            {
                Id = i,
                Properties = Enumerable.Range(1, 10).ToDictionary(
                    j => $"Property{j}",
                    j => $"Value{i}_{j}"
                ),
                Nested = new
                {
                    Values = Enumerable.Range(1, 5).ToArray(),
                    Metadata = new { Created = DateTime.UtcNow.AddDays(-i) }
                }
            }).ToArray()
        };
        _largeJson = JsonSerializer.SerializeToUtf8Bytes(largeObject);
    }

    /// <summary>
    /// Baseline benchmark for deserializing small JSON using <see cref="Utf8JsonAsyncStreamReader"/>.
    /// Tests the complete deserialization of a simple JSON object to measure baseline performance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object or null.</returns>
    [Benchmark(Baseline = true)]
    public async Task<object?> SmallJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_smallJson);
        using var reader = new Utf8JsonAsyncStreamReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    /// <summary>
    /// Benchmark for deserializing medium-sized JSON using <see cref="Utf8JsonAsyncStreamReader"/>.
    /// Tests performance with moderately complex JSON containing arrays of objects.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object or null.</returns>
    [Benchmark]
    public async Task<object?> MediumJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_mediumJson);
        using var reader = new Utf8JsonAsyncStreamReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    /// <summary>
    /// Benchmark for deserializing large JSON using <see cref="Utf8JsonAsyncStreamReader"/>.
    /// Tests performance with complex nested JSON structures containing multiple levels of nesting.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object or null.</returns>
    [Benchmark]
    public async Task<object?> LargeJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_largeJson);
        using var reader = new Utf8JsonAsyncStreamReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    /// <summary>
    /// Benchmark for reading small JSON token-by-token using <see cref="Utf8JsonAsyncStreamReader"/>.
    /// Simulates real-world usage where individual tokens are processed sequentially
    /// without full deserialization, useful for streaming scenarios.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Benchmark]
    public async Task SmallJson_TokenByToken()
    {
        using var stream = new MemoryStream(_smallJson);
        using var reader = new Utf8JsonAsyncStreamReader(stream);
        
        while (await reader.ReadAsync())
        {
            // Process each token (simulate real usage)
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    /// <summary>
    /// Benchmark for reading medium JSON token-by-token using <see cref="Utf8JsonAsyncStreamReader"/>.
    /// Simulates real-world streaming scenarios where tokens are processed individually
    /// for memory-efficient parsing of larger datasets.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Benchmark]
    public async Task MediumJson_TokenByToken()
    {
        using var stream = new MemoryStream(_mediumJson);
        using var reader = new Utf8JsonAsyncStreamReader(stream);
        
        while (await reader.ReadAsync())
        {
            // Process each token (simulate real usage)
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    /// <summary>
    /// Benchmark for deserializing small JSON using the standard <see cref="JsonSerializer"/>.
    /// Provides a performance comparison baseline against the built-in System.Text.Json implementation
    /// to measure the relative performance of the streaming approach.
    /// </summary>
    /// <returns>The deserialized object or null.</returns>
    [Benchmark]
    public object? SmallJson_SystemTextJson()
    {
        return JsonSerializer.Deserialize<object>(_smallJson);
    }

    /// <summary>
    /// Benchmark for deserializing medium JSON using the standard <see cref="JsonSerializer"/>.
    /// Provides a performance comparison baseline against the built-in System.Text.Json implementation
    /// for moderately complex JSON structures.
    /// </summary>
    /// <returns>The deserialized object or null.</returns>
    [Benchmark]
    public object? MediumJson_SystemTextJson()
    {
        return JsonSerializer.Deserialize<object>(_mediumJson);
    }
}