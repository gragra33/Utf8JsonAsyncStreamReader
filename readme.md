# Utf8JsonAsyncStreamReader

An Asynchronous forward-only streaming JSON parser and deserializer based on [System.Text.Json.Utf8JsonReader](https://github.com/dotnet/runtime/blob/418aa8ab6bb5cce2be1a8dee292818d2c201f152/src/libraries/System.Text.Json/src/System/Text/Json/Reader/Utf8JsonReader.cs). Buffer reads a stream and enable conditional branch deserialization. Memory usage is minimal, based of either the buffer size used or the json property branch being deserialized.

See the [Deserializing Json Streams using Newtonsoft.Json & System.Text.Json with C# & VB](https://www.codeproject.com/Articles/5340376/Deserializing-Json-Streams-using-Newtonsoft-and-Sy') article for detailed breakdown and usage with C# or VB.Net with full samples, benchmarks, tests, and more. Covers both file and web streaming with unzipped and zipped files of very large size.

## Nuget

Package: [Utf8JsonAsyncStreamReader](https://www.nuget.org/packages/Utf8JsonAsyncStreamReader)

## Support

If you find this library useful, then please consider [buying me a coffee ☕](https://bmc.link/gragra33).