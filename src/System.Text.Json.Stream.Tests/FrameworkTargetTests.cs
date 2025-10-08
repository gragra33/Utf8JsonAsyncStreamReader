using System.Text.Json;
using System.Text.Json.Stream;
using FluentAssertions;
using JsonReader = System.Text.Json.Stream.Utf8JsonAsyncStreamReader;

namespace UnitTests.Tests.Readers.System.Text.Json;

/// <summary>
/// Tests for verifying framework-specific functionality across different .NET target frameworks.
/// </summary>
[Collection("Sequential")]
public class FrameworkTargetTests
{
    /// <summary>
    /// Verifies that the library works correctly on the current target framework.
    /// </summary>
    [Fact]
    public void Should_Support_Current_Target_Framework()
    {
        // Arrange
        var jsonData = JsonSerializer.Serialize(new { Message = "Hello World", Value = 42 });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
        var reader = new JsonReader(stream);

        // Act & Assert - This test ensures the library works on all target frameworks
        reader.Should().NotBeNull();
        reader.Should().BeAssignableTo<IUtf8JsonAsyncStreamReader>();
        
        // Get current framework
        var framework = global::System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        framework.Should().NotBeNullOrEmpty();
        
        // Dispose properly
        ((IDisposable)reader).Dispose();
    }

    /// <summary>
    /// Verifies that JSON deserialization works correctly across all target frameworks.
    /// </summary>
    [Fact]
    public async Task Should_Read_Json_On_All_Target_Frameworks()
    {
        // Arrange
        var testData = new { Framework = GetTargetFramework(), Version = "1.2.0", IsSupported = true };
        var jsonData = JsonSerializer.Serialize(testData);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
        var reader = new JsonReader(stream);

        try
        {
            // Act
            var result = await reader.DeserializeAsync<object>();

            // Assert
            result.Should().NotBeNull();
        }
        finally
        {
            // Dispose properly
            ((IDisposable)reader).Dispose();
        }
    }

    /// <summary>
    /// Verifies that complex object deserialization works correctly across all target frameworks.
    /// </summary>
    [Fact]
    public async Task Should_Handle_Target_Framework_Specific_Features()
    {
        // Arrange
        var testObject = new FrameworkTestObject
        {
            TargetFramework = GetTargetFramework(),
            Version = "1.2.0",
            SupportedFeatures = new List<string> { "Async", "Streaming", "JSON" }
        };
        
        var jsonData = JsonSerializer.Serialize(testObject);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
        var reader = new JsonReader(stream);

        try
        {
            // Act
            var result = await reader.DeserializeAsync<FrameworkTestObject>();

            // Assert
            result.Should().NotBeNull();
            result!.TargetFramework.Should().Be(GetTargetFramework());
            result.Version.Should().Be("1.2.0");
            result.SupportedFeatures.Should().HaveCount(3);
        }
        finally
        {
            // Dispose properly
            ((IDisposable)reader).Dispose();
        }
    }

    /// <summary>
    /// Gets the current target framework identifier.
    /// </summary>
    /// <returns>A string representing the current target framework.</returns>
    private static string GetTargetFramework()
    {
#if NET9_0
        return "net9.0";
#elif NET8_0
        return "net8.0";
#elif NET7_0
        return "net7.0";
#else
        return "unknown";
#endif
    }
}

/// <summary>
/// Test model for framework-specific testing.
/// </summary>
public class FrameworkTestObject
{
    /// <summary>
    /// Gets or sets the target framework identifier.
    /// </summary>
    public string TargetFramework { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of supported features.
    /// </summary>
    public List<string> SupportedFeatures { get; set; } = new();
}