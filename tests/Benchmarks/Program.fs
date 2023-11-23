// Some verifications and performance tests.
open System
open FSharp.Data.JsonProvider
open FSharp.Data

[<Literal>]
let schemaJson = """[
    {"myRoot": { 
        "name": "Tuomas", "opt": 123.5, "intOrStr": 432, "myObj": { "a": 1 },"myObj2": [1,2,3],"c": null
    }},{ "myRoot": {
        "name": "Tuomas2","intOrStr": "asdf","myObj": { "a": 1 },"myObj2": [4,5,6],"c": null
    }}]"""

[<Literal>]
let sampleJsonLitral = """{"myRoot": { 
        "name": "Tuomas", "opt": 123.5, "intOrStr": 432, "myObj": { "a": 1 },"myObj2": [1,2,3],"c": null
    }}"""

let sampleJson = sampleJsonLitral
type SmallItem = FSharp.Data.JsonProvider<schemaJson, SampleIsList=true>

[<Literal>]
let sampleJsonWithSpecials = """{"myRoot": { 
        "name": "Tuomas", "opt": 123.5, "intOrStr": 432, "myObjÄ": { "a": 1 },"myObj2": [1,2,3],"c": null,
        "testÄ": "Ä", "myDateTime": "2023-10-12 14:00:00", "empty": {}
    }}"""
type SpecialsItem = FSharp.Data.JsonProvider<sampleJsonWithSpecials>

[<Literal>]
let listJson =
    """{"items": [""" + sampleJsonLitral + "] }"
type ListItems = FSharp.Data.JsonProvider<listJson>

Console.WriteLine sampleJson

let testBasics() =

    let readWithProvider_writeWithSystem = 
        let dom = SmallItem.Parse sampleJson
        if (dom.MyRoot.Name <> "Tuomas") then failwith "JsonProvider didn't work"
        Serializer.Serialize (dom.JsonValue)

    let readWithSystem_writeWithProvider = 
        let dom = SmallItem.Load (Serializer.Deserialize sampleJson)
        if (dom.MyRoot.Name <> "Tuomas") then failwith "JsonProvider didn't work"
        dom.JsonValue.ToString(JsonSaveOptions.DisableFormatting)
    
    if (readWithProvider_writeWithSystem <> readWithSystem_writeWithProvider) then
        Console.WriteLine readWithProvider_writeWithSystem
        failwith "Serialization didn't produce equal values"

let testExtended() =
    let specialsPlusExtraStuff = """{"myRoot": { 
        "name": "Tuomas", "opt": 123.5, "intOrStr": 432, "myObjÄ": { "a": 1 },"myObj2": [1,2,3],"c": "notnull",
        "testÄ": "Ä", "myDateTime": "2023-10-12 14:00:00", "empty": {}, "outOfSchemaItem": "x"
    }}"""

    let readWithProvider_writeWithSystem = 
        let dom = SpecialsItem.Parse specialsPlusExtraStuff
        if (dom.MyRoot.Name <> "Tuomas") then failwith "JsonProvider didn't work"
        Serializer.Serialize (dom.JsonValue)

    let readWithSystem_writeWithProvider = 
        let dom = SpecialsItem.Load (Serializer.Deserialize specialsPlusExtraStuff)
        if (dom.MyRoot.Name <> "Tuomas") then failwith "JsonProvider didn't work"
        dom.JsonValue.ToString(JsonSaveOptions.DisableFormatting)
    
    if (readWithProvider_writeWithSystem <> readWithSystem_writeWithProvider) then
        Console.WriteLine readWithProvider_writeWithSystem
        failwith "Serialization didn't produce equal values"

testBasics()
testExtended()
printfn "Basic validation pass. Ready for benchmarking"
//Console.ReadLine() |> ignore


open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs


//[<Literal>] //StripeJson: Download from https://raw.githubusercontent.com/stripe/openapi/master/openapi/spec3.json
//let stripeJson = @"C:\git\FSharp.Data.JsonProvider.Serializer\tests\Benchmarks\spec3.json"
//type HugeJson = FSharp.Data.JsonProvider<stripeJson, SampleIsList=true>

[<SimpleJob (RuntimeMoniker.Net48); MemoryDiagnoser(true)>]
type Benchmark() =

    // Test 1000 small items JSON
    let thousandItems = """{"items": [""" + String.Join(",", [|1 .. 1000|] |> Array.map(fun j -> sampleJson.Replace("432", j.ToString()))) + "] }"

    // Test small JSON

    [<Benchmark()>]
    member this.Serialization_SmallJson_JsonProvider() =
        let dom = SmallItem.Parse sampleJson
        if (dom.MyRoot.Name <> "Tuomas") then failwith "didn't work"
        let res = dom.JsonValue.ToString(JsonSaveOptions.DisableFormatting)
        res.Length

        
    [<Benchmark>]
    member this.Serialization_SmallJson_SystemTextJson() =
        let dom = SmallItem.Load (Serializer.Deserialize sampleJson)
        if (dom.MyRoot.Name <> "Tuomas") then failwith "didn't work"
        let res = Serializer.Serialize (dom.JsonValue)
        res.Length

    [<Benchmark()>]
    member this.Serialization_List1000SmallJson_JsonProvider() =
        let dom = ListItems.Parse thousandItems
        if (dom.Items.[500].MyRoot.Name <> "Tuomas") then failwith "didn't work"
        let res = dom.JsonValue.ToString(JsonSaveOptions.DisableFormatting)
        res.Length

        
    [<Benchmark>]
    member this.Serialization_List1000SmallJson_SystemTextJson() =
        let dom = ListItems.Load (Serializer.Deserialize thousandItems)
        if (dom.Items.[500].MyRoot.Name <> "Tuomas") then failwith "didn't work"
        let res = Serializer.Serialize (dom.JsonValue)
        res.Length

(*
    // Test large JSON

    [<Benchmark()>]
    member this.Serialization_StripeJson_JsonProvider() =
        //let load = System.IO.File.ReadAllBytes(stripeJson)
        let dom = HugeJson.Load(stripeJson)
        if (dom.Paths.V1ApplicationFees.Get.OperationId <> "GetApplicationFees") then failwith "didn't work"
        let res = dom.JsonValue.ToString(JsonSaveOptions.DisableFormatting)
        res.Length
        
    [<Benchmark>]
    member this.Serialization_StripeJson_SystemTextJson() =
        let load = System.ReadOnlySpan(System.IO.File.ReadAllBytes stripeJson)
        let dom = HugeJson.Load (Serializer.DeserializeBytes load)
        if (dom.Paths.V1ApplicationFees.Get.OperationId <> "GetApplicationFees") then failwith "didn't work"
        let res = Serializer.Serialize (dom.JsonValue)
        res.Length
*)

BenchmarkRunner.Run<Benchmark>() |> ignore

(* // For debugging:
(Benchmark()).Serialization_SmallJson_JsonProvider() |> ignore
(Benchmark()).Serialization_SmallJson_SystemTextJson() |> ignore
(Benchmark()).Serialization_List1000SmallJson_JsonProvider() |> ignore
(Benchmark()).Serialization_List1000SmallJson_SystemTextJson() |> ignore
//(Benchmark()).Serialization_StripeJson_JsonProvider() |> ignore
//(Benchmark()).Serialization_StripeJson_SystemTextJson() |> ignore
Console.ReadLine() |> ignore
*)
