# SampleAot - AOT-Compatible Console Application

This sample demonstrates how to use `Utf8JsonAsyncStreamReader` in a Native AOT compiled application.

## Features

? **Fully AOT-Compatible** - Uses source-generated `JsonSerializerContext`  
? **Token-by-Token Reading** - No special configuration needed  
? **Object Deserialization** - With proper AOT configuration  
? **Array Processing** - Efficient streaming of large JSON arrays

## Building and Running

### Prerequisites for Native AOT

To publish with Native AOT on Windows, you need:

1. **Visual Studio 2022** with the "Desktop development with C++" workload
   - Or **Visual Studio Build Tools** with C++ components
2. See the [Native AOT Prerequisites](https://aka.ms/nativeaot-prerequisites) for detailed installation instructions

**On Linux:** Install clang and developer packages  
**On macOS:** Install Xcode command line tools

### Standard Build and Run (No Prerequisites Required)

```bash
dotnet build
dotnet run
```

This works without any additional prerequisites and demonstrates that the code is AOT-compatible.

### Verify AOT Compatibility Without Publishing

You can verify the code is AOT-compatible without installing C++ build tools:

```bash
dotnet build /p:PublishAot=true
```

This will show AOT warnings (if any) without actually creating a native binary.

### Publish with Native AOT

**[!] Prerequisites must be installed first (see above)**

**Windows (x64):**
```bash
dotnet publish -c Release -r win-x64 -f net10.0
```

**Windows (ARM64):**
```bash
dotnet publish -c Release -r win-arm64 -f net10.0
```

**Linux (x64):**
```bash
dotnet publish -c Release -r linux-x64 -f net10.0
```

**Linux (ARM64):**
```bash
dotnet publish -c Release -r linux-arm64 -f net10.0
```

**macOS (x64):**
```bash
dotnet publish -c Release -r osx-x64 -f net10.0
```

**macOS (ARM64 - Apple Silicon):**
```bash
dotnet publish -c Release -r osx-arm64 -f net10.0
```

The published executable will be a single, self-contained binary with no .NET runtime dependency.

## Examples Included

### 1. Token-by-Token Reading
Shows how to read JSON token-by-token without deserialization. This approach is always AOT-safe and requires no special configuration.

```csharp
while (await reader.ReadAsync())
{
    switch (reader.TokenType)
    {
        case JsonTokenType.PropertyName:
            Console.WriteLine($"Property: {reader.GetString()}");
            break;
        case JsonTokenType.String:
            Console.WriteLine($"String: {reader.GetString()}");
            break;
        // ...
    }
}
```

### 2. Object Deserialization
Demonstrates deserializing JSON to strongly-typed objects using `JsonSerializerContext` for AOT compatibility.

```csharp
var options = new JsonSerializerOptions
{
    TypeInfoResolver = AppJsonContext.Default
};

var person = await reader.DeserializeAsync<Person>(options);
```

### 3. Array Processing
Efficiently processes large JSON arrays by streaming and deserializing one item at a time to minimize memory usage.

```csharp
await reader.ReadAsync(); // StartArray
while (await reader.ReadAsync() && reader.TokenType != JsonTokenType.EndArray)
{
    var person = await reader.DeserializeAsync<Person>(options);
    ProcessPerson(person);
}
```

## Key AOT Concepts

### JsonSerializerContext (Required for AOT)

The `JsonSerializerContext` must declare all types that will be serialized/deserialized:

```csharp
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(List<Person>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
internal partial class AppJsonContext : JsonSerializerContext
{
}
```

### Using the Context

Always pass the `JsonSerializerOptions` configured with your context:

```csharp
var options = new JsonSerializerOptions
{
    TypeInfoResolver = AppJsonContext.Default
};

var result = await reader.DeserializeAsync<Person>(options);
```

### Suppressing AOT Warnings

When using `DeserializeAsync` with `JsonSerializerOptions` (instead of `JsonTypeInfo<T>`), you may see IL2026 and IL3050 warnings. These can be safely suppressed when you're properly configuring the context:

```csharp
[UnconditionalSuppressMessage("Trimming", "IL2026:...")]
[UnconditionalSuppressMessage("AOT", "IL3050:...")]
static async Task DeserializeExample(JsonSerializerOptions options)
{
    var result = await reader.DeserializeAsync<Person>(options);
}
```

## Project Configuration

The project file (`SampleAot.csproj`) includes:

- `<PublishAot>true</PublishAot>` - Enables Native AOT compilation
- `<InvariantGlobalization>true</InvariantGlobalization>` - Reduces binary size
- `<Nullable>enable</Nullable>` - Enables nullable reference types

## Performance Benefits

Native AOT compilation provides:

- [FAST] **Faster startup** - No JIT compilation needed (50-100x faster)
- [SMALL] **Smaller deployment** - Single executable with trimmed dependencies
- [MEM] **Lower memory usage** - No runtime metadata overhead
- [SECURE] **Better security** - Harder to decompile
- [SAFE] **Reduced attack surface** - Only necessary code is included

## Troubleshooting

### Error: Platform linker not found

**Problem:** `error Platform linker not found...` when running `dotnet publish`.

**Solution:** Install Visual Studio 2022 with "Desktop development with C++" workload, or install Visual Studio Build Tools with C++ components.

See: https://aka.ms/nativeaot-prerequisites

### Warning: IL2026 or IL3050

**Problem:** Seeing IL2026 or IL3050 warnings during build.

**Solution:** If you're properly using `JsonSerializerContext` through `TypeInfoResolver`, these warnings can be safely suppressed with `[UnconditionalSuppressMessage]` attributes (as shown in the code).

## Output Example

```
*** AOT-Compatible Utf8JsonAsyncStreamReader Sample
===================================================

Example 1: Token-by-Token Reading (AOT-Safe)
---------------------------------------------
  Property: name
    String Value: John Doe
  Property: age
    Number Value: 30
  Property: email
    String Value: john@example.com
  Property: isActive
    Boolean Value: True

Example 2: Deserialization with AOT Support
-------------------------------------------
  JSON: {"id":1,"name":"Jane Smith","email":"jane@example.com","age":28}
  Deserialized: Jane Smith, Age: 28

Example 3: Process Large Array
-------------------------------
  Person 1: Alice Johnson, Age: 25, Email: alice@example.com
  Person 2: Bob Williams, Age: 30, Email: bob@example.com
  Person 3: Charlie Brown, Age: 35, Email: charlie@example.com
  Total people processed: 3

[OK] All examples completed successfully!

[INFO] This code is fully AOT-compatible and can be published with:
   dotnet publish -c Release -r win-x64 -f net10.0
   dotnet publish -c Release -r linux-x64 -f net10.0
   dotnet publish -c Release -r osx-x64 -f net10.0

[!] Note: Native AOT requires C++ build tools to be installed.
   See: https://aka.ms/nativeaot-prerequisites
```

## Learn More

- See the [AOT_GUIDE.md](../../AOT_GUIDE.md) for comprehensive documentation on AOT compatibility
- [System.Text.Json Source Generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)
- [Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Native AOT Prerequisites](https://aka.ms/nativeaot-prerequisites)
