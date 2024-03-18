module MicroBenchmarks.FSharp.Program

open System
open System.IO
open System.Collections.Immutable

open BenchmarkDotNet.Running
open BenchmarkDotNet.Extensions

[<EntryPoint>]
let main args =
    BenchmarkSwitcher
        .FromAssembly(typeof<Collections.CollectionsBenchmark>.Assembly)
        .Run(args, RecommendedConfig.Create(
            artifactsPath = DirectoryInfo(
                Path.Combine(
                    AppContext.BaseDirectory, 
                    "BenchmarkDotNet.Artifacts")),
            mandatoryCategories = ImmutableHashSet.Create Categories.FSharpMicroCategory))
        .ToExitCode()
