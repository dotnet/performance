namespace FSharpBenchmarks

open System
open System.Collections.Immutable
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open BenchmarkDotNet.Attributes
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.AbstractIL

[<RequireQualifiedAccess>]
module Sources =

    let helloWorld =
        """
[<EntryPoint>]
let main _argv =
    printfn "Hello World!"
    0
        """

/// FSharp compiler service benchmarks
[<BenchmarkCategory("FSharp");MemoryDiagnoser>]
type FSharpCompilerServiceBenchmarks () =

    let sourceDir = Path.Combine(Environment.CurrentDirectory, "fsharpSource");
    let tempFsFile = Path.Combine(sourceDir, "temp.fs")
    let tempOutputFile = Path.Combine(sourceDir, "temp.dll")

    let defaultReferenceArgs =
        Helpers.getNet5References ()
        |> Array.map (fun r -> "-r:" + r)

    let compileArgs = 
        Array.append
            [|
                "--preferreduilang:en-US"
                "--optimize+"
                "--langversion:preview"
                "-o:" + tempOutputFile
                "--nowin32manifest"
                "--noframework"
                "--simpleresolution"
                "--targetprofile:netcore"
                "--warn:5"
                tempFsFile
            |]
            defaultReferenceArgs

    let mutable checker = Unchecked.defaultof<FSharpChecker>

    [<GlobalSetup>]
    member _.Setup() =
        Directory.CreateDirectory(sourceDir) |> ignore

        // Benchmark.NET creates a new process to run the benchmark, so the easiest way
        // to communicate information is pass by environment variable
        Environment.SetEnvironmentVariable(Helpers.testProjectEnvVarName, sourceDir)

        match box checker with
        | null ->
            checker <- FSharpChecker.Create(projectCacheSize = 200)
        | _ ->
            ()

    [<IterationSetup(Target = "CompileHelloWorld")>]
    member _.CompileHelloWorldSetup() =
        File.WriteAllText(tempFsFile, Sources.helloWorld)

    [<Benchmark>]
    member _.CompileHelloWorld() =
        let errors, _ = checker.Compile(compileArgs) |> Async.RunSynchronously
        if errors.Length > 0 then
            failwithf "%A" errors

    [<IterationCleanup(Target = "CompileHelloWorld")>]
    member _.CompileHelloWorldCleanup() =
        checker.InvalidateAll()
        checker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
        try File.Delete(tempOutputFile) with | _ -> ()
        try File.Delete(tempFsFile) with | _ -> ()
