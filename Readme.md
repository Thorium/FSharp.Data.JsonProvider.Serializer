
# FSharp.Data.JsonProvider.Serializer

NuGet package: https://www.nuget.org/packages/FSharp.Data.JsonProvider.Serializer/
This is not independent package, you still use your current JsonProvider.

This will provide utilities to use the fast `System.Text.Json` library to serialize the `FSharp.Data.JsonProvider` items.

 - FSharp.Data: https://fsprojects.github.io/FSharp.Data/
 - System.Text.Json: https://www.nuget.org/packages/System.Text.Json

Motivation: Serialization speed.
Typically JSON Serialization is used either so that the user is watching a progress-bar, or in a big batch-process.

Current FSharp.Data is using custom Json-serializer.

We need a compromise having the convinience of F# JsonProvider, but speed of System.Text.Json.

The idea is to be in-replacement for current functions:

## Reading values to type provider (Deserialization)

Current FSharp.Data.JsonProvider:

```fsharp
type MyJsonType = FSharp.Data.JsonProvider<"""{ "model": "..." } """>

let fromJson (response:string) = MyJsonType.Parse response

```

Using this library:

```fsharp
type MyJsonType = FSharp.Data.JsonProvider<"""{ "model": "..." } """>

let fromJson (response:string) = MyJsonType.Load (Serializer.Deserialize response)

```


## Saving values from type provider (Serialization)

Current FSharp.Data.JsonProvider:

```fsharp
type MyJson = FSharp.Data.JsonProvider<"""{ "model": "..." } """>

let toJson mymodel = mymodel.JsonValue.ToString()

```

Using this library:

```fsharp
type MyJson = FSharp.Data.JsonProvider<"""{ "model": "..." } """>

let toJson mymodel = Serializer.Serialize (mymodel.JsonValue)

```

Besides of this, you continue using your existing JsonProvider implementation as is.
Currently it uses System.Text.Json style of encoding the quote characters, etc. but if you want to customize
your serialization more, you can set JsonReaderOptions and JsonWriterOptions as parameters.

## Initial Benchmarks

FSharp.Data 6.3, System.Text.Json 8.0

Test-case:
- Read JSON to JsonProvider. (Serialization)
- Verify a property
- Save JSON back to a string. (Deserialization)

Tested with small JSON file, and with Stripe OpenAPI spec file (+5MB of JSON).

BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2715/22H2/2022Update/SunValley2)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores



#### .NET 8.0 : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

| Method                                 | Mean          | Error         | StdDev        | Gen0      | Gen1      | Gen2      | Allocated   |
|--------------------------------------- |--------------:|--------------:|--------------:|----------:|----------:|----------:|------------:|
| SmallJson_JsonProvider                 |      2.789 μs |     0.0314 μs |     0.0293 μs |    0.4120 |         - |         - |     5.06 KB |
| SmallJson_SystemTextJson               |      1.623 μs |     0.0308 μs |     0.0288 μs |    0.2651 |         - |         - |     3.27 KB |
| ListOf1000SmallJsons_JsonProvider      |  2,849.210 μs |    25.9458 μs |    20.2568 μs |  351.5625 |  175.7813 |   58.5938 |   4224.8 KB |
| ListOf1000SmallJsons_SystemTextJson    |  1,424.314 μs |     9.3599 μs |     8.7553 μs |  199.2188 |  199.2188 |  199.2188 |  2469.76 KB |
| StripeJson_JsonProvider                | 83,610.123 μs | 1,667.5590 μs | 3,406.3812 μs | 5285.7143 | 4571.4286 | 1714.2857 | 60521.87 KB |
| StripeJson_SystemTextJsonBytes         | 42,775.639 μs |   822.2020 μs | 1,009.7378 μs | 2333.3333 | 2166.6667 |  750.0000 | 44305.47 KB |



#### .NET Framework 4.8 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256

| Method                                 | Mean           | Error         | StdDev        | Gen0      | Gen1      | Gen2      | Allocated   |
|--------------------------------------- |---------------:|--------------:|--------------:|----------:|----------:|----------:|------------:|
| SmallJson_JsonProvider                 |       6.613 μs |     0.0554 μs |     0.0518 μs |    0.8621 |         - |         - |     5.32 KB |
| SmallJson_SystemTextJson               |       4.832 μs |     0.0542 μs |     0.0480 μs |    0.5493 |         - |         - |      3.4 KB |
| ListOf1000SmallJsons_JsonProvider      |   7,052.777 μs |    53.6100 μs |    47.5238 μs |  695.3125 |  343.7500 |   54.6875 |   4465.7 KB |
| ListOf1000SmallJsons_SystemTextJson    |   4,453.736 μs |    43.9446 μs |    41.1058 μs |  390.6250 |  195.3125 |  195.3125 |  2587.36 KB |
| StripeJson_JsonProvider                | 107,016.899 μs | 2,094.9813 μs | 3,500.2432 μs | 8200.0000 | 3800.0000 | 1400.0000 | 61612.22 KB |
| StripeJson_SystemTextJsonBytes         |  77,002.389 μs | 1,531.7106 μs | 2,559.1443 μs | 4285.7143 | 2285.7143 | 1000.0000 | 45245.97 KB |


To run the test: dotnet run --project tests\Benchmarks\BenchmarkTests.fsproj --configuration=Release --framework=net8.0


## API

FSharp.Data uses internally class called `FSharp.Data.JsonValue` to model the JSON domain.

#### From JSON to JsonProvider

- `Deserialize`: string to FSharp.Data.JsonValue
- `DeserializeWith`: string and System.Text.Json.JsonReaderOptions to FSharp.Data.JsonValue
- `DeserializeBytes`: byte array to FSharp.Data.JsonValue
- `DeserializeBytesWith`: byte array and System.Text.Json.JsonReaderOptions to FSharp.Data.JsonValue

#### From JsonProvider to JSON

- `Serialize`: FSharp.Data.JsonValue to string
- `SerializeWith`: FSharp.Data.JsonValue and System.Text.Json.JsonWriterOptions to string
- `SerializeBytes`: FSharp.Data.JsonValue to byte array
- `SerializeBytesWith`: FSharp.Data.JsonValue and System.Text.Json.JsonWriterOptions to byte array

#### From JsonProvider to JSON, Streaming

With streaming support you can send JSON-data to output stream record-per-record.

- `SerializeStream`: destination stream, FSharp.Data.JsonValue
- `SerializeStreamWith`: destination stream, FSharp.Data.JsonValue and System.Text.Json.JsonWriterOptions

Streaming may keep around the same performance characteristics, but reduce a memory allocation a bit and provide partially consumable results faster if JSON is large and the client supports streaming.

**.NET 8.0 : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2**

| Method                                    | Mean          | Error         | StdDev        | Gen0      | Gen1      | Gen2      | Allocated   |
|------------------------------------------ |--------------:|--------------:|--------------:|----------:|----------:|----------:|------------:|
| ListOf1000SmallJsons_SystemTextJsonStream |  1,525.970 μs |    14.0992 μs |    13.1884 μs |  158.2031 |  117.1875 |   78.1250 |  2100.46 KB |
| StripeJson_SystemTextJsonStream           | 47,112.065 μs |   805.5929 μs |   753.5521 μs | 2333.3333 | 2250.0000 |  750.0000 | 31560.25 KB |

**.NET Framework 4.8 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256**

| Method                                    | Mean           | Error         | StdDev        | Gen0      | Gen1      | Gen2      | Allocated   |
|------------------------------------------ |---------------:|--------------:|--------------:|----------:|----------:|----------:|------------:|
| ListOf1000SmallJsons_SystemTextJsonStream |   4,650.089 μs |    34.2087 μs |    30.3251 μs |  312.5000 |  156.2500 |   78.1250 |  2217.05 KB |
| StripeJson_SystemTextJsonStream           |  76,921.557 μs | 1,448.2545 μs | 1,778.5866 μs | 4000.0000 | 2000.0000 | 1000.0000 | 32501.88 KB |


