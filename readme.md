# Utf8JsonAsyncStreamReader

[![NuGet Version](https://img.shields.io/nuget/v/Utf8JsonAsyncStreamReader.svg)](https://www.nuget.org/packages/Utf8JsonAsyncStreamReader)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Utf8JsonAsyncStreamReader.svg)](https://www.nuget.org/packages/Utf8JsonAsyncStreamReader)
[![.NET 8+](https://img.shields.io/badge/.NET-8%2B-512BD4)](https://dotnet.microsoft.com/download)

## Table of Contents

- [Overview](#overview)
- [Getting Started](#getting-started)
  - [Installation](#installation)
  - [Requirements](#requirements)
- [Usage](#usage)
  - [Basic Stream Reading](#basic-stream-reading)
  - [Object Deserialization](#object-deserialization)
  - [Custom JsonSerializerOptions](#custom-jsonserializeroptions)
  - [Processing Large Collections](#processing-large-collections)
  - [Exception Handling](#exception-handling)
- [Value Methods](#value-methods)
- [How It Works](#how-it-works)
- [Samples](#samples)
- [v1.3.0 Performance Graphs](#v130-performance-graphs)
  - [Speed Improvement by Scenario](#speed-improvement-by-scenario)
  - [Gen2 GC Reduction](#gen2-gc-reduction)
- [Real-World Impact](#real-world-impact)
  - [High-Throughput API Example](#high-throughput-api-example)
- [Support](#support)
- [History](#history)

---

## Overview

An asynchronous forward-only streaming JSON parser and deserializer based on [System.Text.Json.Utf8JsonReader](https://github.com/dotnet/runtime/blob/418aa8ab6bb5cce2be1a8dee292818d2c201f152/src/libraries/System.Text.Json/src/System/Text/Json/Reader/Utf8JsonReader.cs). This library enables efficient processing of large JSON streams with minimal memory usage by buffering stream reads and supporting conditional branch deserialization. Memory consumption is optimized based on either the buffer size used or the specific JSON property branch being deserialized.

Perfect for scenarios involving:
- Large JSON file processing
- Web API streaming responses  
- Memory-constrained environments
- Real-time data processing

## Getting Started

### Installation

Install the package via NuGet Package Manager:

```xml
<PackageReference Include="Utf8JsonAsyncStreamReader" Version="1.2.0" />
```

Or via the Package Manager Console:

```powershell
Install-Package Utf8JsonAsyncStreamReader
```

Or via the .NET CLI:

```bash
dotnet add package Utf8JsonAsyncStreamReader
```

### Requirements

- .NET 8.0, .NET 9.0, or .NET 10.0
- System.IO.Pipelines (automatically included)

## Usage

The Utf8JsonAsyncStreamReader provides a forward-only, memory-efficient approach to processing JSON streams of any size. Whether you're reading from files, HTTP responses, or other stream sources, the reader maintains minimal memory footprint by processing data incrementally rather than loading entire documents into memory.

### Basic Stream Reading

```csharp
using System.Text.Json.Stream;

// Create a stream (file, HTTP response, etc.)
using var fileStream = File.OpenRead("large-data.json");
using var reader = new Utf8JsonAsyncStreamReader(fileStream);

// Read JSON tokens asynchronously with cancellation support
using var cts = new CancellationTokenSource();
while (await reader.ReadAsync(cts.Token))
{
    switch (reader.TokenType)
    {
        case JsonTokenType.PropertyName:
            Console.WriteLine($"Property: {reader.Value}");
            break;
        case JsonTokenType.String:
            Console.WriteLine($"String Value: {reader.Value}");
            break;
        case JsonTokenType.Number:
            Console.WriteLine($"Number Value: {reader.Value}");
            break;
        // Handle other token types...
    }
}
```

### Object Deserialization

```csharp
using System.Text.Json.Stream;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

// Deserialize directly from stream with cancellation support
using var stream = GetJsonStream(); // Your JSON stream source
using var reader = new Utf8JsonAsyncStreamReader(stream);
using var cts = new CancellationTokenSource();

var person = await reader.DeserializeAsync<Person>(cancellationToken: cts.Token);
Console.WriteLine($"Name: {person.Name}, Age: {person.Age}");
```

### Custom JsonSerializerOptions

```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};

using var reader = new Utf8JsonAsyncStreamReader(stream);
using var cts = new CancellationTokenSource();
var result = await reader.DeserializeAsync<MyObject>(options, cts.Token);
```

### Processing Large Collections

```csharp
// For large JSON arrays, process items one by one
using var reader = new Utf8JsonAsyncStreamReader(stream);
using var cts = new CancellationTokenSource();

await reader.ReadAsync(cts.Token); // StartArray
while (await reader.ReadAsync(cts.Token) && reader.TokenType != JsonTokenType.EndArray)
{
    var item = await reader.DeserializeAsync<MyItem>(cancellationToken: cts.Token);
    ProcessItem(item); // Process each item individually
}
```

### Exception Handling

The library provides specific exception types to help you handle different error scenarios:

```csharp
using System.Text.Json.Stream;

try
{
    using var reader = new Utf8JsonAsyncStreamReader(stream);
    using var cts = new CancellationTokenSource();
    
    while (await reader.ReadAsync(cts.Token))
    {
        // Process tokens...
    }
}
catch (JsonStreamException ex)
{
    // Handle JSON stream-specific errors:
    // - Invalid JSON structure
    // - Incomplete tokens
    // - Buffer undersized for current JSON structure
    Console.WriteLine($"JSON Stream Error: {ex.Message}");
}
catch (JsonException ex)
{
    // Handle general JSON serialization errors
    Console.WriteLine($"JSON Error: {ex.Message}");
}
catch (OperationCanceledException)
{
    // Handle cancellation
    Console.WriteLine("Operation was cancelled");
}
```

## Value Methods

The `Utf8JsonHelpers` class provides comprehensive extension methods to extract strongly-typed values from JSON tokens. These methods follow the standard .NET patterns and offer both safe (Try*) and direct (Get*) access patterns.

### String & Copy Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetString()` | `string?` | Gets the string value, returns null for JSON null tokens |
| `CopyString(Span<byte>)` | `int` | Copies UTF-8 bytes to destination span, throws if buffer too small |
| `CopyString(Span<char>)` | `int` | Copies transcoded UTF-16 chars to destination span, throws if buffer too small |

### Boolean Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetBoolean()` | `bool?` | Gets the boolean value (true/false), returns null for non-boolean tokens |

### Numeric Methods - Byte Types

| Method | Returns | Description |
|--------|---------|-------------|
| `GetByte()` | `byte` | Gets byte value (0-255), returns 0 on failure |
| `TryGetByte(out byte?)` | `bool` | Attempts to get byte value, returns success status |
| `GetSByte()` | `sbyte` | Gets signed byte value (-128 to 127), returns 0 on failure |
| `TryGetSByte(out sbyte?)` | `bool` | Attempts to get signed byte value, returns success status |
| `GetUByte()` | `uint` | Gets unsigned byte value, returns 0 on failure |
| `TryGetUByte(out uint?)` | `bool` | Attempts to get unsigned byte value, returns success status |

### Numeric Methods - Integer Types

| Method | Returns | Description |
|--------|---------|-------------|
| `GetInt16()` | `short` | Gets 16-bit signed integer (-32,768 to 32,767), returns 0 on failure |
| `TryGetInt16(out short?)` | `bool` | Attempts to get 16-bit signed integer, returns success status |
| `GetUInt16()` | `uint` | Gets 16-bit unsigned integer (0 to 65,535), returns 0 on failure |
| `TryGetUInt16(out uint?)` | `bool` | Attempts to get 16-bit unsigned integer, returns success status |
| `GetInt32()` | `int` | Gets 32-bit signed integer, returns 0 on failure |
| `TryGetInt32(out int?)` | `bool` | Attempts to get 32-bit signed integer, returns success status |
| `GetUInt32()` | `uint` | Gets 32-bit unsigned integer, returns 0 on failure |
| `TryGetUInt32(out uint?)` | `bool` | Attempts to get 32-bit unsigned integer, returns success status |
| `GetInt64()` | `long` | Gets 64-bit signed integer, returns 0 on failure |
| `TryGetInt64(out long?)` | `bool` | Attempts to get 64-bit signed integer, returns success status |
| `GetUInt64()` | `ulong` | Gets 64-bit unsigned integer, returns 0 on failure |
| `TryGetUInt64(out ulong?)` | `bool` | Attempts to get 64-bit unsigned integer, returns success status |

### Numeric Methods - Floating Point Types

| Method | Returns | Description |
|--------|---------|-------------|
| `GetSingle()` | `float` | Gets single-precision floating-point number, returns 0 on failure |
| `TryGetSingle(out float?)` | `bool` | Attempts to get single-precision floating-point number, returns success status |
| `GetDouble()` | `double` | Gets double-precision floating-point number, returns 0 on failure |
| `TryGetDouble(out double?)` | `bool` | Attempts to get double-precision floating-point number, returns success status |
| `GetDecimal()` | `decimal` | Gets decimal value, returns 0 on failure |
| `TryGetDecimal(out decimal?)` | `bool` | Attempts to get decimal value, returns success status |

### Date/Time Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetDateTime()` | `DateTime` | Gets DateTime value, returns default on failure |
| `TryGetDateTime(out DateTime?)` | `bool` | Attempts to get DateTime value, returns success status |
| `GetDateTimeOffset()` | `DateTimeOffset` | Gets DateTimeOffset value, returns default on failure |
| `TryGetDateTimeOffset(out DateTimeOffset?)` | `bool` | Attempts to get DateTimeOffset value, returns success status |

### GUID Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetGuid()` | `Guid` | Gets Guid value, returns Empty on failure |
| `TryGetGuid(out Guid?)` | `bool` | Attempts to get Guid value, returns success status |

### Base64 Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetBytesFromBase64()` | `byte[]?` | Decodes Base64 string to bytes, throws FormatException on failure |
| `TryGetBytesFromBase64(out byte[]?)` | `bool` | Attempts to decode Base64 string, returns success status |

### Value Extraction Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetValue()` | `object?` | Gets typed value based on TokenType (string, number, bool, null) |
| `GetNumber()` | `object?` | Parses number token to best-fit numeric type (Int16, Int32, Int64, Double) |

## How It Works

For a detailed explanation of the internal architecture, performance optimizations, and streaming mechanisms, see our comprehensive [How It Works](HOW_IT_WORKS.md) documentation, with mermaid diagrams.

This guide covers:

- Core streaming architecture and `System.IO.Pipelines` integration
- Memory management and buffer optimization strategies  
- Asynchronous processing patterns and cancellation handling
- Performance characteristics and benchmarking methodology
- Real-world usage patterns and best practices

## Samples

Explore these comprehensive examples and tutorials:

- **[Blazor JSON Streaming Sample](https://github.com/gragra33/BlazorJsonStreamingSample)** - Complete Blazor application demonstrating real-time JSON streaming
- **[CodeProject Article: Deserializing Json Streams](https://codeproject.com/articles/Deserializing-Json-Streams-using-Newtonsoft-and-Sy)** - Detailed breakdown with C# & VB.NET examples, benchmarks, and performance comparisons

These resources include:
- Full working samples
- Performance benchmarks  
- Memory usage comparisons
- File and web streaming examples
- Support for compressed (zipped) files
- Handling very large datasets

## v1.3.0 Performance Graphs

Version 1.3.0 introduces significant performance optimizations that deliver measurable improvements across all JSON processing scenarios. These enhancements focus on reducing memory allocations and garbage collection pressure while increasing overall throughput.

### Speed Improvement by Scenario
```
Small JSON Token-by-Token:     +7.65% ========·· ✨
Medium JSON Token-by-Token:    +9.89% =========· ✨✨
Large JSON Token-by-Token:    +19.61% ========== ✨✨✨
```

### Gen2 GC Reduction
```
Before: ========================================  (490 collections)
After:  ===================·····················  (235 collections)
```

That is a **52% REDUCTION!** ✨✨✨

## Real-World Impact

These performance improvements translate directly into cost savings and improved user experience in production environments. Lower memory usage reduces infrastructure requirements, while fewer garbage collections mean more predictable response times and higher system stability.

### High-Throughput API Example
**Scenario**: API processing 10,000 large JSON documents per second

#### Before (v1.2.0)
- Processing Time: 56.46 seconds per 10K documents
- Memory Allocations: ~28.47 GB per 10K documents
- Gen2 GC: ~4,900 collections per 10K documents

#### After (v1.3.0)
- Processing Time: **45.39 seconds** per 10K documents
- Memory Allocations: ~27.09 GB per 10K documents  
- Gen2 GC: **~2,350 collections** per 10K documents

#### Savings
- **==· Time**: 11.07 seconds saved per 10K documents (**+19.61% throughput!**)
- **==· Memory**: 1.38 GB less per 10K documents (**+4.82% efficiency**)
- **=== Gen2 GC**: 2,550 fewer collections per 10K documents (**-52% reduction!**)
- **==· Cost**: Reduced infrastructure needs, fewer GC pauses
- **==· Capacity**: Can handle **20% more concurrent requests** with same resources

## Support

If you find this library useful, please consider [buying me a coffee ☕](https://bmc.link/gragra33).

## History

### v2.1.0 - 19 November 2025

- Added new Value methods: CopyString (performant UTF8 & UTF16), GetSByte, TryGetSByte, GetBytesFromBase64, TryGetBytesFromBase64
- Updated readme with tables of all Value converter methods
- Added test coverage for new Value methods

### v2.0.0 - 17 November 2025

- Added support for .Net 10
- Removed support for .Net 7

### v1.3.1 - October 2025

- Added custom `JsonStreamException` for better error handling specificity
- Added comprehensive [How It Works](HOW_IT_WORKS.md) documentation (uses mermaid diagrams)

### v1.3.0 - October 2025

- Performance improvements
  - Up to **19.61% faster**
  - **52% less** Gen2 GC collections
  - **4-6% less** memory usage
- Added Benchmarks

### V1.2.0 - October 2025

- Added .NET 9.0 support
- Added symbols support to NuGet package  
- Updated readme with improved documentation
- Enhanced test coverage for all target frameworks

### V1.1.0 - Previous Release

- Updated to support .NET 8.0
- Fixed missing parameter JsonSerializerOptions in one call

### V1.0.0 - Initial Release

- Initial release with .NET 7.0 support
- Core streaming JSON functionality
- Basic deserialization capabilities
