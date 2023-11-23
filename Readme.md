
# FSharp.Data.JsonProvider.Serializer

NuGet package: https://www.nuget.org/packages/FSharp.Data.JsonProvider.Serializer/

This will provide utilities to use the fast `System.Text.Json` library to serialize the `FSharp.Data.JsonProvider`` items.

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

### .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2 DEBUG

| Method                                         | Mean           | Error         | StdDev        | Gen0      | Gen1      | Gen2      | Allocated   |
|----------------------------------------------- |---------------:|--------------:|--------------:|----------:|----------:|----------:|------------:|
| Serialization_SmallJson_JsonProvider           |       6.690 μs |     0.0676 μs |     0.0632 μs |    0.8621 |         - |         - |     5.32 KB |
| Serialization_SmallJson_SystemTextJson         |       5.110 μs |     0.0538 μs |     0.0477 μs |    0.5798 |         - |         - |     3.59 KB |
| Serialization_List1000SmallJson_JsonProvider   |   7,133.210 μs |    55.7108 μs |    49.3862 μs |  695.3125 |  343.7500 |   54.6875 |   4465.7 KB |
| Serialization_List1000SmallJson_SystemTextJson |   4,857.725 μs |    54.6552 μs |    51.1245 μs |  992.1875 |  195.3125 |  195.3125 |  6727.73 KB |
| Serialization_StripeJson_JsonProvider          | 106,183.427 μs | 1,839.6922 μs | 2,118.5922 μs | 8200.0000 | 3800.0000 | 1400.0000 | 61611.32 KB |
| Serialization_StripeJson_SystemTextJson        |  77,737.344 μs | 1,336.4109 μs | 1,312.5342 μs | 6857.1429 | 2857.1429 | 1000.0000 | 61055.16 KB |

### .NET Framework 4.8 : .NET Framework 4.8.1 (4.8.9181.0), X64 RyuJIT VectorSize=256

| Method                                         | Mean           | Error         | StdDev        | Gen0      | Gen1      | Gen2      | Allocated   |
|----------------------------------------------- |---------------:|--------------:|--------------:|----------:|----------:|----------:|------------:|
| Serialization_SmallJson_JsonProvider           |       6.734 μs |     0.1130 μs |     0.1002 μs |    0.8621 |         - |         - |     5.32 KB |
| Serialization_SmallJson_SystemTextJson         |       5.022 μs |     0.0470 μs |     0.0440 μs |    0.5798 |         - |         - |     3.59 KB |
| Serialization_List1000SmallJson_JsonProvider   |   7,073.432 μs |    44.3017 μs |    41.4398 μs |  695.3125 |  343.7500 |   54.6875 |   4465.7 KB |
| Serialization_List1000SmallJson_SystemTextJson |   4,799.585 μs |    20.7208 μs |    18.3684 μs |  992.1875 |  195.3125 |  195.3125 |  6727.73 KB |
| Serialization_StripeJson_JsonProvider          | 107,795.785 μs | 2,093.3960 μs | 2,865.4641 μs | 8200.0000 | 3800.0000 | 1400.0000 |  61614.1 KB |
| Serialization_StripeJson_SystemTextJson        |  79,231.282 μs | 1,565.7823 μs | 1,863.9524 μs | 6857.1429 | 2857.1429 | 1000.0000 | 61048.42 KB |

To run the test: dotnet run --project tests\Benchmarks\BenchmarkTests.fsproj --configuration=Release --framework=net8.0
