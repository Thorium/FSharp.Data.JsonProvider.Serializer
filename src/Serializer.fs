namespace FSharp.Data.JsonProvider

open System.Runtime.CompilerServices
open System
open System.Text
open System.Text.Json
open FSharp.Data
open System.IO

module internal SerializationFunctions =

    // Estimate initial capacity for arrays based on JSON token type and depth
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline estimateArrayCapacity () = 8  // Reasonable default for most JSON arrays
    
    // Estimate initial capacity for objects based on JSON structure
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline estimateObjectCapacity () = 4  // Reasonable default for most JSON objects

    let rec constructArray (reader: byref<Utf8JsonReader>) (built: ResizeArray<JsonValue>) =
        if not (reader.Read()) then
            built
        else
            match reader.TokenType with
            | JsonTokenType.EndArray -> built
            | _ ->
                match handleToken &reader with
                | ValueSome thisToken ->
                    built.Add thisToken
                    constructArray &reader built
                | ValueNone -> built

    and constructRecord (reader: byref<Utf8JsonReader>) (nextName: string) (built: ResizeArray<string * JsonValue>) =
        if not (reader.Read()) then
            built
        else
            match reader.TokenType with
            | JsonTokenType.EndObject -> built
            | JsonTokenType.PropertyName ->
                let name = reader.GetString()
                constructRecord &reader name built
            | _ ->
                match handleToken &reader with
                | ValueSome thisToken ->

                    built.Add(nextName, thisToken)
                    constructRecord &reader nextName built
                | ValueNone -> built

    and handleToken (reader: byref<Utf8JsonReader>) =
        match reader.TokenType with
        | JsonTokenType.Number ->
            // Try decimal first for better precision, fallback to double
            if reader.TryGetDecimal() |> fst then
                ValueSome(JsonValue.Number(reader.GetDecimal()))
            else
                ValueSome(JsonValue.Float(reader.GetDouble()))
        | JsonTokenType.Null -> ValueSome JsonValue.Null
        | JsonTokenType.String -> ValueSome(JsonValue.String(reader.GetString()))
        | JsonTokenType.Comment
        | JsonTokenType.None -> ValueNone
        | JsonTokenType.True -> ValueSome(JsonValue.Boolean true)
        | JsonTokenType.False -> ValueSome(JsonValue.Boolean false)
        | JsonTokenType.StartArray ->
            let array = constructArray &reader (ResizeArray(estimateArrayCapacity()))
            ValueSome(JsonValue.Array(array.ToArray()))
        | JsonTokenType.StartObject ->
            let record = constructRecord &reader "" (ResizeArray(estimateObjectCapacity()))
            ValueSome(JsonValue.Record(record.ToArray()))

        | x -> failwithf $"Unexpected json: {x}"


    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline read (contentBytes: inref<ReadOnlySpan<byte>>) (options: inref<JsonReaderOptions>) =
        let mutable fullreader = Utf8JsonReader(contentBytes, options)
        let nextToken = fullreader.Read()

        if not nextToken then
            JsonValue.Null
        else

            match handleToken &fullreader with
            | ValueSome token -> token
            | ValueNone -> JsonValue.Null


    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let rec writeJsonValue (writer: Utf8JsonWriter) (content: JsonValue) (stream: bool) =
        match content with
        | JsonValue.Null -> writer.WriteNullValue()
        | JsonValue.Boolean b -> writer.WriteBooleanValue b
        | JsonValue.String s -> writer.WriteStringValue s
        | JsonValue.Float f -> writer.WriteNumberValue f
        | JsonValue.Number n -> writer.WriteNumberValue n
        | JsonValue.Array a ->
            writer.WriteStartArray()

            for itm in a do
                writeJsonValue writer itm stream

            writer.WriteEndArray()
        | JsonValue.Record r ->
            writer.WriteStartObject()

            for (name, value) in r do
                writer.WritePropertyName name
                writeJsonValue writer value stream

            writer.WriteEndObject()

            // Only flush for stream mode and only after completing a top-level record
            if stream then
                writer.Flush()

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline write (item: JsonValue) (options: inref<JsonWriterOptions>) =
        use stream = new MemoryStream(256) // Pre-size with reasonable default
        use writer = new Utf8JsonWriter(stream, options)
        writeJsonValue writer item false
        writer.Flush()
        stream.ToArray()

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline writeStream (destination: Stream) (item: JsonValue) (options: inref<JsonWriterOptions>) =
        use writer = new Utf8JsonWriter(destination, options)
        writeJsonValue writer item true
        writer.Flush()

module Serializer =

    open System.Text.Encodings
    open System.Text.Unicode

    /// Options for reading file with System.Text.Json
    #if !NETS20
    [<System.Runtime.CompilerServices.IsReadOnly>]
    #endif
    let mutable internal default_read_options =
        JsonReaderOptions(AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip)

    /// Options for writing file with System.Text.Json
    #if !NETS20
    [<System.Runtime.CompilerServices.IsReadOnly>]
    #endif
    let mutable internal default_write_options =
        JsonWriterOptions( // Trying to match current FSharp.Data better:
            Encoder = Web.JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement)
        )

    // Helper to minimize allocations in string-to-byte conversion
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let inline private stringToUtf8Bytes (str: string) =
        if String.IsNullOrEmpty(str) then
            ReadOnlySpan<byte>.Empty
        else
            ReadOnlySpan(Encoding.UTF8.GetBytes str)

    /// Deserialize UTF8 stream to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let DeserializeBytes (jsonUtf8Bytes: ReadOnlySpan<byte>) =
        SerializationFunctions.read &jsonUtf8Bytes &default_read_options

    /// Deserialize UTF8 stream to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let DeserializeBytesWith (jsonUtf8Bytes: ReadOnlySpan<byte>, options: JsonReaderOptions) =
        SerializationFunctions.read &jsonUtf8Bytes &options

    /// Deserialize string to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let Deserialize (item: string) =
        let content = stringToUtf8Bytes item
        SerializationFunctions.read &content &default_read_options

    /// Deserialize string to FSharp.Data.JsonValue using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let DeserializeWith (item: string, options: JsonReaderOptions) =
        let content = stringToUtf8Bytes item
        SerializationFunctions.read &content &options


    /// Serialize FSharp.Data.JsonValue to byte array using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let SerializeBytes (item: JsonValue) =
        SerializationFunctions.write item &default_write_options

    /// Serialize FSharp.Data.JsonValue to byte array using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let SerializeBytesWith (item: JsonValue, options: JsonWriterOptions) =
        SerializationFunctions.write item &options

    /// Serialize FSharp.Data.JsonValue to Stream using System.Text.Json
    /// Will flush per each JSON record written.
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let SerializeStream (destination: Stream, item: JsonValue) =
        SerializationFunctions.writeStream destination item &default_write_options

    /// Serialize FSharp.Data.JsonValue to Stream using System.Text.Json
    /// Will flush per each JSON record written.
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let SerializeStreamWith (destination: Stream, item: JsonValue, options: JsonWriterOptions) =
        SerializationFunctions.writeStream destination item &options


    /// Serialize FSharp.Data.JsonValue to string using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let Serialize (item: JsonValue) =
        Encoding.UTF8.GetString(SerializationFunctions.write item &default_write_options)

    /// Serialize FSharp.Data.JsonValue to string using System.Text.Json
    [<Extension>]
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let SerializeWith (item: JsonValue, options: JsonWriterOptions) =
        Encoding.UTF8.GetString(SerializationFunctions.write item &options)
