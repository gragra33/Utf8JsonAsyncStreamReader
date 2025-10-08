using BenchmarkDotNet.Attributes;

// Alias for the old NuGet version (v1.2.0) via wrapper project
using OldReader = System.Text.Json.Stream.Old.Utf8JsonAsyncStreamReaderOld;
// Alias for the new local version
using NewReader = System.Text.Json.Stream.Utf8JsonAsyncStreamReader;

namespace System.Text.Json.Stream.Benchmarks;

/// <summary>
/// Benchmark comparing the old Utf8JsonAsyncStreamReader (v1.2.0 from NuGet) 
/// with the new optimized version.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
public class OldVsNewComparisonBenchmark
{
    private byte[] _smallJson = null!;
    private byte[] _mediumJson = null!;
    private byte[] _largeJson = null!;
    
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

    #region Small JSON Benchmarks

    [Benchmark(Baseline = true, Description = "Old v1.2.0 - Small JSON DeserializeAsync")]
    public async Task<object?> Old_SmallJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_smallJson);
        using var reader = new OldReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    [Benchmark(Description = "New Optimized - Small JSON DeserializeAsync")]
    public async Task<object?> New_SmallJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_smallJson);
        using var reader = new NewReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    [Benchmark(Description = "Old v1.2.0 - Small JSON Token-by-Token")]
    public async Task Old_SmallJson_TokenByToken()
    {
        using var stream = new MemoryStream(_smallJson);
        using var reader = new OldReader(stream);
        
        while (await reader.ReadAsync())
        {
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    [Benchmark(Description = "New Optimized - Small JSON Token-by-Token")]
    public async Task New_SmallJson_TokenByToken()
    {
        using var stream = new MemoryStream(_smallJson);
        using var reader = new NewReader(stream);
        
        while (await reader.ReadAsync())
        {
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    #endregion

    #region Medium JSON Benchmarks

    [Benchmark(Description = "Old v1.2.0 - Medium JSON DeserializeAsync")]
    public async Task<object?> Old_MediumJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_mediumJson);
        using var reader = new OldReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    [Benchmark(Description = "New Optimized - Medium JSON DeserializeAsync")]
    public async Task<object?> New_MediumJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_mediumJson);
        using var reader = new NewReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    [Benchmark(Description = "Old v1.2.0 - Medium JSON Token-by-Token")]
    public async Task Old_MediumJson_TokenByToken()
    {
        using var stream = new MemoryStream(_mediumJson);
        using var reader = new OldReader(stream);
        
        while (await reader.ReadAsync())
        {
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    [Benchmark(Description = "New Optimized - Medium JSON Token-by-Token")]
    public async Task New_MediumJson_TokenByToken()
    {
        using var stream = new MemoryStream(_mediumJson);
        using var reader = new NewReader(stream);
        
        while (await reader.ReadAsync())
        {
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    #endregion

    #region Large JSON Benchmarks

    [Benchmark(Description = "Old v1.2.0 - Large JSON DeserializeAsync")]
    public async Task<object?> Old_LargeJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_largeJson);
        using var reader = new OldReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    [Benchmark(Description = "New Optimized - Large JSON DeserializeAsync")]
    public async Task<object?> New_LargeJson_DeserializeAsync()
    {
        using var stream = new MemoryStream(_largeJson);
        using var reader = new NewReader(stream);
        return await reader.DeserializeAsync<object>();
    }

    [Benchmark(Description = "Old v1.2.0 - Large JSON Token-by-Token")]
    public async Task Old_LargeJson_TokenByToken()
    {
        using var stream = new MemoryStream(_largeJson);
        using var reader = new OldReader(stream);
        
        while (await reader.ReadAsync())
        {
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    [Benchmark(Description = "New Optimized - Large JSON Token-by-Token")]
    public async Task New_LargeJson_TokenByToken()
    {
        using var stream = new MemoryStream(_largeJson);
        using var reader = new NewReader(stream);
        
        while (await reader.ReadAsync())
        {
            _ = reader.TokenType;
            _ = reader.Value;
        }
    }

    #endregion
}
