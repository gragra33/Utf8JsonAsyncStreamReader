namespace UnitTests.Tests.Readers.System.Text.Json.Models;

/// <summary>
/// Test model for JSON property deserialization containing various data types.
/// </summary>
public class JsonPropertyObject
{
    /// <summary>
    /// Gets or sets a positive integer value.
    /// </summary>
    public int? IntPositive { get; set; }
    
    /// <summary>
    /// Gets or sets a negative integer value.
    /// </summary>
    public int? IntNegative { get; set; }
    
    /// <summary>
    /// Gets or sets a positive long value.
    /// </summary>
    public long? LongPositive { get; set; }
    
    /// <summary>
    /// Gets or sets a negative long value.
    /// </summary>
    public long? LongNegative { get; set; }
    
    /// <summary>
    /// Gets or sets a positive short value.
    /// </summary>
    public short? ShortPositive { get; set; }
    
    /// <summary>
    /// Gets or sets a negative short value.
    /// </summary>
    public short? ShortNegative { get; set; }
    
    /// <summary>
    /// Gets or sets a positive float value.
    /// </summary>
    public float? FloatPositive { get; set; }
    
    /// <summary>
    /// Gets or sets a negative float value.
    /// </summary>
    public float? FloatNegative { get; set; }
    
    /// <summary>
    /// Gets or sets a positive double value.
    /// </summary>
    public double? DoublePositive { get; set; }
    
    /// <summary>
    /// Gets or sets a negative double value.
    /// </summary>
    public double? DoubleNegative { get; set; }
    
    /// <summary>
    /// Gets or sets a boolean value.
    /// </summary>
    public bool? BoolValue { get; set; }
    
    /// <summary>
    /// Gets or sets a string value.
    /// </summary>
    public string? StringValue { get; set; }
    
    /// <summary>
    /// Gets or sets a DateTime value.
    /// </summary>
    public DateTime? DateTimeValue { get; set; }
}