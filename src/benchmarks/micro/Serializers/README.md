# Serializers Benchmarks

This folder contains benchmarks of the most popular serializers.

## Serializers used (latest stable versions)

* XML
  * [XmlSerializer](https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer) `4.3.0`
* JSON
    * [DataContractJsonSerializer](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.json.datacontractjsonserializer) `4.3.0`
    * [Jil](https://github.com/kevin-montrose/Jil) `2.17.0` 
    * [JSON.NET](https://github.com/JamesNK/Newtonsoft.Json) `12.0.2` 
    * [Utf8Json](https://github.com/neuecc/Utf8Json) `1.3.7` 
* Binary
    * [BinaryFormatter](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.formatters.binary.binaryformatter) `4.3.0`
    * [MessagePack](https://github.com/neuecc/MessagePack-CSharp) `1.7.3.7` 
    * [protobuff-net](https://github.com/mgravell/protobuf-net) `2.4.0`

Missing: ProtoBuff from Google and BOND from MS

## Data Contracts

Data Contracts were copied from a real Web App � [allReady](https://github.com/HTBox/allReady/) to mimic real world scenarios.

* [LoginViewModel](DataGenerator.cs#L120) � class, 3 properties
* [Location](DataGenerator.cs#L133) � class, 9 properties
* [IndexViewModel](DataGenerator.cs#L202) � class, nested class + list of 20 Events (8 properties each)
* [MyEventsListerViewModel](DataGenerator.cs#L224) - class, 3 lists of complex types, each type contains another list of complex types

## Design Decisions

1. We want to compare "apples to apples", so the benchmarks are divided into few groups: `ToStream`, `FromStream`, `ToString`, `FromString`.
2. Stream benchmarks write to pre-allocated MemoryStream, so the allocated bytes columns include only the cost of serialization. 
