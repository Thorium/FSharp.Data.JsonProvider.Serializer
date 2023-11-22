
# FSharp.Data.JsonProvider.Serializer

NuGet package: https://www.nuget.org/packages/FSharp.Data.JsonProvider.Serializer/

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

## Initial Benchmarks

Test-case:
- Read JSON to JsonProvider. (Serialization)
- Verify a property
- Save JSON back to a string. (Deserialization)

Tested with small JSON file, and with Stripe OpenAPI spec file (+5MB of JSON).

BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2715/22H2/2022Update/SunValley2)
13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores

### .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG

| Method                                  | Mean           | Error         | StdDev        | Median         | Gen0      | Gen1      | Gen2      | Allocated   |
|---------------------------------------- |---------------:|--------------:|--------------:|---------------:|----------:|----------:|----------:|------------:|
| Serialization_SmallJson_JsonProvider    |       7.050 us |     0.2193 us |     0.6465 us |       6.616 us |    0.8545 |         - |         - |     5.32 KB |
| Serialization_SmallJson_SystemTextJson  |       5.000 us |     0.0416 us |     0.0369 us |       4.998 us |    0.5798 |         - |         - |     3.59 KB |
| Serialization_StripeJson_JsonProvider   | 110,650.331 us | 1,939.4994 us | 1,619.5699 us | 111,154.500 us | 8200.0000 | 3800.0000 | 1400.0000 | 61615.76 KB |
| Serialization_StripeJson_SystemTextJson |  79,382.557 us | 1,570.6070 us | 1,469.1468 us |  79,597.929 us | 6857.1429 | 2857.1429 | 1000.0000 |  61057.1 KB |

### .NET Framework 4.8 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256

| Method                                  | Mean           | Error         | StdDev        | Gen0      | Gen1      | Gen2      | Allocated   |
|---------------------------------------- |---------------:|--------------:|--------------:|----------:|----------:|----------:|------------:|
| Serialization_SmallJson_JsonProvider    |       7.132 us |     0.0773 us |     0.0685 us |    0.8621 |         - |         - |     5.32 KB |
| Serialization_SmallJson_SystemTextJson  |       4.938 us |     0.0664 us |     0.0621 us |    0.5798 |         - |         - |     3.59 KB |
| Serialization_StripeJson_JsonProvider   | 107,985.207 us | 1,474.9305 us | 1,151.5288 us | 8200.0000 | 3800.0000 | 1400.0000 | 61615.82 KB |
| Serialization_StripeJson_SystemTextJson |  81,017.954 us | 1,565.8860 us | 2,295.2554 us | 6857.1429 | 2857.1429 | 1000.0000 | 61057.03 KB |

