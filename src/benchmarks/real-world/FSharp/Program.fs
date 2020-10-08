open System
open System.Collections.Immutable
open System.Reflection
open System.IO
open System.Threading.Tasks
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Extensions

open FSharpBenchmarks

let init argv =
    async {
        let asm = Assembly.GetExecutingAssembly()
        return
            BenchmarkSwitcher.FromAssembly(asm)
             .Run(argv, RecommendedConfig.Create(
                            artifactsPath = DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "BenchmarkDotNet.Artifacts")),
                            mandatoryCategories = ImmutableHashSet.Create("FSharp"),
                            job = Job.Default.WithMaxRelativeError(0.01)))
             .ToExitCode()                        
    }

[<EntryPoint>]
let main argv =
    init argv
    |> Async.RunSynchronously