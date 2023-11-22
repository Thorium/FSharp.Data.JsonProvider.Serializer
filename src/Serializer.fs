namespace FSharp.Data.JsonProvider

open System.Runtime.CompilerServices
open System
open System.Text
open System.Text.Json
open FSharp.Data
open System.IO

module internal SerializationFunctions =

    let defaultoptions = 
        JsonReaderOptions(
                    AllowTrailingCommas = true, 
                    CommentHandling = JsonCommentHandling.Skip)

    let rec constructArray (reader: byref<Utf8JsonReader>) (built:JsonValue[]) =
        if not (reader.Read()) then built
        else
            match reader.TokenType with
            | JsonTokenType.EndArray ->
                built
            | _ -> 
                match handleToken &reader with 
                | ValueSome thisToken ->
                    let nextGen = Array.append built [|thisToken|]
                    constructArray &reader nextGen
                | ValueNone -> built

    and constructRecord (reader: byref<Utf8JsonReader>) (nextName:string) (built:(string*JsonValue)[]) =
        if not (reader.Read()) then built
        else
            match reader.TokenType with
            | JsonTokenType.EndObject ->
                built
            | JsonTokenType.PropertyName ->
                let name = reader.GetString()
                // todo, combine name and value
                constructRecord &reader name built
            | _ -> 
                match handleToken &reader with 
                | ValueSome thisToken ->
                    
                    let nextGen = Array.append built [|nextName,thisToken|]
                    constructRecord &reader nextName nextGen
                | ValueNone -> built

    and handleToken (reader: byref<Utf8JsonReader>) =
        match reader.TokenType with
        | JsonTokenType.Number -> 
            match reader.TryGetDecimal() with
            | true, deci -> ValueSome (JsonValue.Number (reader.GetDecimal()))
            | false, floa -> ValueSome (JsonValue.Float (reader.GetDouble()))
        | JsonTokenType.Null -> ValueSome JsonValue.Null
        | JsonTokenType.String -> 
            ValueSome (JsonValue.String (reader.GetString()))
        | JsonTokenType.Comment
        | JsonTokenType.None -> ValueNone
        | JsonTokenType.True -> ValueSome (JsonValue.Boolean true)
        | JsonTokenType.False -> ValueSome (JsonValue.Boolean false)
        | JsonTokenType.StartArray ->
            let array = constructArray &reader [||]
            ValueSome (JsonValue.Array array)
        | JsonTokenType.StartObject -> 
            let record = constructRecord &reader "" [||]
            ValueSome (JsonValue.Record record)


        | x -> failwithf $"Unexpected json: {x}" // Other token types elided for brevity


    let read (contentBytes:ReadOnlySpan<byte>)  (options:JsonReaderOptions) = 
        let mutable fullreader = Utf8JsonReader(contentBytes, options)
        let nextToken = fullreader.Read()
        if not nextToken then JsonValue.Null
        else 

        match handleToken &fullreader with
        | ValueSome token -> token
        | ValueNone -> JsonValue.Null


    let write (item:JsonValue) (options:JsonWriterOptions voption) =
        use stream = new MemoryStream();
        use writer = 
            match options with
            | ValueSome opt -> new Utf8JsonWriter(stream, opt)
            | ValueNone -> new Utf8JsonWriter(stream)

        let rec write' content =
            match content with
            | JsonValue.Null -> writer.WriteNullValue()
            | JsonValue.Boolean b -> writer.WriteBooleanValue b
            | JsonValue.String s -> writer.WriteStringValue s
            | JsonValue.Float f -> writer.WriteNumberValue f
            | JsonValue.Number n -> writer.WriteNumberValue n
            | JsonValue.Array a -> 
                writer.WriteStartArray()
                for itm in a do
                    write' itm
                writer.WriteEndArray()
            | JsonValue.Record r -> 
                writer.WriteStartObject()
                for (name,value) in r do
                    writer.WritePropertyName name
                    write' value
                writer.WriteEndObject()
        write' item
        writer.Flush();
        stream.ToArray()

module Serializer =

    /// Deserialize UTF8 stream to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    let DeserializeBytes (jsonUtf8Bytes:ReadOnlySpan<byte>) =
        SerializationFunctions.read jsonUtf8Bytes SerializationFunctions.defaultoptions

    /// Deserialize UTF8 stream to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    let DeserializeBytesWith (jsonUtf8Bytes:ReadOnlySpan<byte>, options:JsonReaderOptions) =
        SerializationFunctions.read jsonUtf8Bytes options

    /// Deserialize string to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    let Deserialize (item:string) =
        SerializationFunctions.read (System.ReadOnlySpan (Encoding.UTF8.GetBytes item)) SerializationFunctions.defaultoptions

    /// Deserialize string to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    let DeserializeWith (item:string, options:JsonReaderOptions) =
        SerializationFunctions.read (System.ReadOnlySpan (Encoding.UTF8.GetBytes item)) options


    /// Serialize FSharp.Data.JsonValue to byte array using System.Text.Json
    [<Extension>]
    let SerializeBytes (item:JsonValue) =
        SerializationFunctions.write item ValueNone

    /// Serialize FSharp.Data.JsonValue to byte array using System.Text.Json
    [<Extension>]
    let SerializeBytesWith (item:JsonValue, options:JsonWriterOptions) =
        SerializationFunctions.write item (ValueSome options)

    /// Serialize FSharp.Data.JsonValue to string using System.Text.Json
    [<Extension>]
    let Serialize (item:JsonValue) =
        Encoding.UTF8.GetString(SerializationFunctions.write item ValueNone)

    /// Serialize FSharp.Data.JsonValue to string using System.Text.Json
    [<Extension>]
    let SerializeWith (item:JsonValue, options:JsonWriterOptions) =
        Encoding.UTF8.GetString(SerializationFunctions.write item (ValueSome options))
        
