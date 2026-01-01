# AOT (Ahead-of-Time) Compilation Guide

This library is **AOT-compatible** and can be used in Native AOT published applications. However, due to the use of `System.Text.Json` deserialization, you must follow specific patterns to ensure compatibility.

## Quick Start for AOT

### 1. Define Your JSON Context

Create a source-generated `JsonSerializerContext` for your types:

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(List<Person>))]
[JsonSerializable(typeof(ApiResponse))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class MyJsonContext : JsonSerializerContext
{
}
```

### 2. Use the Context with Utf8JsonAsyncStreamReader

```csharp
using System.Text.Json;
using System.Text.Json.Stream;

// Configure options with your JSON context
var options = new JsonSerializerOptions
{
    TypeInfoResolver = MyJsonContext.Default
};

// Use with the reader
using var stream = File.OpenRead("data.json");
using var reader = new Utf8JsonAsyncStreamReader(stream);

var person = await reader.DeserializeAsync<Person>(options);
```

## Complete Example

### Define Your Models

```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Person> Data { get; set; } = new();
}
```

### Create JSON Context

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(List<Person>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppJsonContext : JsonSerializerContext
{
}
```

### Use in Your Application

```csharp
using System.Text.Json;
using System.Text.Json.Stream;

class Program
{
    static async Task Main(string[] args)
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonContext.Default
        };

        // Read a single object
        using var fileStream = File.OpenRead("person.json");
        using var reader = new Utf8JsonAsyncStreamReader(fileStream);
        var person = await reader.DeserializeAsync<Person>(options);
        
        Console.WriteLine($"Name: {person?.Name}, Age: {person?.Age}");

        // Process large array efficiently
        using var largeStream = File.OpenRead("large-data.json");
        using var arrayReader = new Utf8JsonAsyncStreamReader(largeStream);
        
        await arrayReader.ReadAsync(); // StartArray
        while (await arrayReader.ReadAsync() && 
               arrayReader.TokenType != JsonTokenType.EndArray)
        {
            var item = await arrayReader.DeserializeAsync<Person>(options);
            ProcessPerson(item);
        }
    }

    static void ProcessPerson(Person? person)
    {
        if (person is not null)
        {
            Console.WriteLine($"Processing: {person.Name}");
        }
    }
}
```

## Publishing with AOT

### Important: Library vs Application

**This library (`Utf8JsonAsyncStreamReader`) is AOT-compatible** but should **not** be published with AOT itself.

- [OK] The **library** is marked with `<IsAotCompatible>true</IsAotCompatible>`
- [X] The **library** should NOT have `<PublishAot>true</PublishAot>`
- [OK] Your **consuming application** should have `<PublishAot>true</PublishAot>`

### Project File Configuration

**Your consuming application** project file should have:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Utf8JsonAsyncStreamReader" Version="2.2.0" />
  </ItemGroup>
</Project>
```

### Publish Commands

**For .NET 10.0:**
```bash
dotnet publish -c Release -r win-x64 -f net10.0
```

**For .NET 9.0:**
```bash
dotnet publish -c Release -r win-x64 -f net9.0
```

**For .NET 8.0:**
```bash
dotnet publish -c Release -r win-x64 -f net8.0
```

**Linux (replace `win-x64` with `linux-x64`):**
```bash
dotnet publish -c Release -r linux-x64 -f net10.0
```

**macOS (use `osx-x64` or `osx-arm64`):**
```bash
dotnet publish -c Release -r osx-x64 -f net10.0
dotnet publish -c Release -r osx-arm64 -f net10.0
```

**Note:** Use the `-f` or `--framework` parameter to specify which target framework to publish for when your application is multi-targeted.

### Sample Application

See the [SampleAot](src/samples/SampleAot) project for a complete working example of an AOT-compatible console application using this library. The sample includes:

- Token-by-token JSON reading (always AOT-safe)
- Object deserialization with `JsonSerializerContext`
- Efficient large array processing
- Complete build and publish instructions

## Token-by-Token Reading (Always AOT-Safe)

If you only need to read JSON token-by-token without deserialization, no special AOT configuration is needed:

```csharp
using var stream = File.OpenRead("data.json");
using var reader = new Utf8JsonAsyncStreamReader(stream);

while (await reader.ReadAsync())
{
    switch (reader.TokenType)
    {
        case JsonTokenType.PropertyName:
            Console.WriteLine($"Property: {reader.Value}");
            break;
        case JsonTokenType.String:
            Console.WriteLine($"String: {reader.Value}");
            break;
        case JsonTokenType.Number:
            Console.WriteLine($"Number: {reader.Value}");
            break;
        // Handle other token types...
    }
}
```

All the value extraction methods in `Utf8JsonHelpers` are AOT-safe:

```csharp
// These are all AOT-compatible
var str = reader.GetString();
var num = reader.GetInt32();
var dbl = reader.GetDouble();
var guid = reader.GetGuid();
var dt = reader.GetDateTime();
// etc.
```

## AOT Warnings

If you don't use a `JsonSerializerContext`, you'll see warnings like:

```
warning IL2026: Using member 'System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)' 
which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```

To fix this, always provide a `JsonSerializerOptions` with a `TypeInfoResolver` as shown above.

## Best Practices for AOT

1. **Always use JsonSerializerContext** - Define all your types in a source-generated context
2. **Pre-configure JsonSerializerOptions** - Create a shared instance with your context
3. **Use specific types** - Avoid `object` or `dynamic` types in your models
4. **Test your AOT build** - Publish with `PublishAot=true` during development to catch issues early
5. **Use token reading for flexibility** - When deserialization isn't needed, use token-by-token reading

## Performance Benefits

Using source-generated contexts not only enables AOT compatibility but also provides:

- **Faster startup** - No runtime reflection or type discovery
- **Smaller binary size** - Only required code is included
- **Better performance** - Source-generated code is optimized
- **Compile-time safety** - Errors caught during build, not runtime

## Common Issues

### Issue: IL2026 Warning

**Problem**: You're getting trim warnings about `DeserializeAsync`.

**Solution**: Add a `JsonSerializerContext` and pass options with `TypeInfoResolver`.

### Issue: Runtime Type Not Found

**Problem**: AOT-published app crashes with type not found error.

**Solution**: Ensure all types are registered in your `JsonSerializerContext` with `[JsonSerializable]` attributes.

### Issue: Polymorphic Types

**Problem**: Need to deserialize polymorphic JSON.

**Solution**: Use `[JsonDerivedType]` attributes:

```csharp
[JsonSerializable(typeof(Shape))]
[JsonDerivedType(typeof(Circle), "circle")]
[JsonDerivedType(typeof(Rectangle), "rectangle")]
internal partial class ShapesContext : JsonSerializerContext
{
}
```

## Additional Resources

- [System.Text.Json Source Generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)
- [Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [.NET Trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options)
