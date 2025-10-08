namespace UnitTests.Tests.Readers.System.Text.Json.Models;

/// <summary>
/// Test model for JSON collection deserialization containing an integer and a list of strings.
/// </summary>
public class JsonCollectionObject
{
    /// <summary>
    /// Gets or sets a positive integer value.
    /// </summary>
    public int? IntPositive { get; set; }

    /// <summary>
    /// Gets or sets a list of string items.
    /// </summary>
    public List<string>? ListItems { get; set; }
}