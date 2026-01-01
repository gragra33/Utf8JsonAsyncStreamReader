using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Stream;

// Define data models
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

// Create JSON context for AOT - this is REQUIRED for Native AOT compilation
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(List<Person>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
internal partial class AppJsonContext : JsonSerializerContext
{
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("AOT-Compatible Utf8JsonAsyncStreamReader Sample");
        Console.WriteLine("===============================================\n");

        // Configure JSON options with source-generated context (REQUIRED for AOT)
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonContext.Default,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Example 1: Token-by-token reading (always AOT-safe, no context needed)
        Console.WriteLine("Example 1: Token-by-Token Reading (AOT-Safe)");
        Console.WriteLine("---------------------------------------------");
        await TokenByTokenExample();

        // Example 2: Deserialize with AOT support
        Console.WriteLine("\nExample 2: Deserialization with AOT Support");
        Console.WriteLine("-------------------------------------------");
        await DeserializeExample(options);

        // Example 3: Process large array efficiently
        Console.WriteLine("\nExample 3: Process Large Array");
        Console.WriteLine("-------------------------------");
        await ProcessArrayExample(options);

        Console.WriteLine("\n[OK] All examples completed successfully!");
        Console.WriteLine("\n[INFO] This code is fully AOT-compatible and can be published with:");
        Console.WriteLine("   dotnet publish -c Release -r win-x64 -f net10.0");
        Console.WriteLine("   dotnet publish -c Release -r linux-x64 -f net10.0");
        Console.WriteLine("   dotnet publish -c Release -r osx-x64 -f net10.0");
        Console.WriteLine("\n[!]  Note: Native AOT requires C++ build tools to be installed.");
        Console.WriteLine("   See: https://aka.ms/nativeaot-prerequisites");
    }

    static async Task TokenByTokenExample()
    {
        var json = """
        {
            "name": "John Doe",
            "age": 30,
            "email": "john@example.com",
            "isActive": true
        }
        """;

using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new Utf8JsonAsyncStreamReader(stream);

        while (await reader.ReadAsync())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    Console.WriteLine($"  Property: {reader.GetString()}");
                    break;
                case JsonTokenType.String:
                    Console.WriteLine($"    String Value: {reader.GetString()}");
                    break;
                case JsonTokenType.Number:
                    Console.WriteLine($"    Number Value: {reader.GetInt32()}");
                    break;
                case JsonTokenType.True or JsonTokenType.False:
                    Console.WriteLine($"    Boolean Value: {reader.GetBoolean()}");
                    break;
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "JsonSerializerOptions is configured with AppJsonContext.Default which provides all required type information for AOT compilation.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "JsonSerializerOptions is configured with AppJsonContext.Default which provides all required type information for AOT compilation.")]
    static async Task DeserializeExample(JsonSerializerOptions options)
    {
        var person = new Person
        {
            Id = 1,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Age = 28
        };

        // Serialize to JSON using the context directly (AOT-safe, no warnings)
        var json = JsonSerializer.Serialize(person, AppJsonContext.Default.Person);
        Console.WriteLine($"  JSON: {json}");

        // Deserialize using Utf8JsonAsyncStreamReader with AOT support
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new Utf8JsonAsyncStreamReader(stream);

        var deserializedPerson = await reader.DeserializeAsync<Person>(options);
        
        if (deserializedPerson != null)
        {
            Console.WriteLine($"  Deserialized: {deserializedPerson.Name}, Age: {deserializedPerson.Age}");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "JsonSerializerOptions is configured with AppJsonContext.Default which provides all required type information for AOT compilation.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "JsonSerializerOptions is configured with AppJsonContext.Default which provides all required type information for AOT compilation.")]
    static async Task ProcessArrayExample(JsonSerializerOptions options)
    {
        var people = new List<Person>
        {
            new() { Id = 1, Name = "Alice Johnson", Email = "alice@example.com", Age = 25 },
            new() { Id = 2, Name = "Bob Williams", Email = "bob@example.com", Age = 30 },
            new() { Id = 3, Name = "Charlie Brown", Email = "charlie@example.com", Age = 35 }
        };

        // Serialize to JSON using the context directly (AOT-safe, no warnings)
        var json = JsonSerializer.Serialize(people, AppJsonContext.Default.ListPerson);
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var reader = new Utf8JsonAsyncStreamReader(stream);

        // Read array start
        await reader.ReadAsync(); // StartArray
        
        int count = 0;
        while (await reader.ReadAsync() && reader.TokenType != JsonTokenType.EndArray)
        {
            var person = await reader.DeserializeAsync<Person>(options);
            if (person != null)
            {
                count++;
                Console.WriteLine($"  Person {count}: {person.Name}, Age: {person.Age}, Email: {person.Email}");
            }
        }

        Console.WriteLine($"  Total people processed: {count}");
    }
}
