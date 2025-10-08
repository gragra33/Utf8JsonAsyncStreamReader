# Utf8JsonAsyncStreamReader

[![NuGet Version](https://img.shields.io/nuget/v/Utf8JsonAsyncStreamReader.svg)](https://www.nuget.org/packages/Utf8JsonAsyncStreamReader)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Utf8JsonAsyncStreamReader.svg)](https://www.nuget.org/packages/Utf8JsonAsyncStreamReader)

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

- .NET 7.0, .NET 8.0, or .NET 9.0
- System.IO.Pipelines (automatically included)

## Usage

### Basic Stream Reading

```csharp
using System.Text.Json.Stream;

// Create a stream (file, HTTP response, etc.)
using var fileStream = File.OpenRead("large-data.json");
using var reader = new Utf8JsonAsyncStreamReader(fileStream);

// Read JSON tokens asynchronously
while (await reader.ReadAsync())
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

// Deserialize directly from stream
using var stream = GetJsonStream(); // Your JSON stream source
using var reader = new Utf8JsonAsyncStreamReader(stream);

var person = await reader.DeserializeAsync<Person>();
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
var result = await reader.DeserializeAsync<MyObject>(options);
```

### Processing Large Collections

```csharp
// For large JSON arrays, process items one by one
using var reader = new Utf8JsonAsyncStreamReader(stream);

await reader.ReadAsync(); // StartArray
while (await reader.ReadAsync() && reader.TokenType != JsonTokenType.EndArray)
{
    var item = await reader.DeserializeAsync<MyItem>();
    ProcessItem(item); // Process each item individually
}
```

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

## Support

If you find this library useful, please consider [buying me a coffee ☕](https://bmc.link/gragra33).

## History

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
