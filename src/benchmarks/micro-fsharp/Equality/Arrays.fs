module MicroBenchmarks.FSharp.Equality.Arrays

open BenchmarkDotNet.Attributes
open MicroBenchmarks.FSharp

[<MemoryDiagnoser>]
[<BenchmarkCategory(Categories.FSharpMicroCategory)>]
type Arrays() =

    let numbers = Array.init 1000 id

    [<Benchmark>]
    member _.Int32() =
        numbers |> Array.countBy  (fun n -> [| n % 7 |])

    [<Benchmark>]
    member _.Int64() =
        numbers |> Array.countBy  (fun n -> [| int64 (n % 7) |])

    [<Benchmark>]
    member _.Byte() =
        numbers |> Array.countBy  (fun n -> [| byte (n % 7) |])

    [<Benchmark>]
    member _.Obj() =
        numbers |> Array.countBy  (fun n -> [| box (n % 7) |])
