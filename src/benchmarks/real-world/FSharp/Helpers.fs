module FSharpBenchmarks.Helpers

open System
open System.IO
open System.Diagnostics

let testProjectEnvVarName = "FSHARP_TEST_PROJECT_DIR"

let getNet5References () =

    let programFs = """
open System
    
[<EntryPoint>]
let main argv = 0"""
    
    let projectFile = """
<Project Sdk="Microsoft.NET.Sdk">
    
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>netcoreapp5.0</TargetFramework>
        <UseFSharpPreview>true</UseFSharpPreview>
    </PropertyGroup>
    
    <ItemGroup><Compile Include="Program.fs" /></ItemGroup>
    
    <Target Name="WriteFrameworkReferences" AfterTargets="AfterBuild">
        <WriteLinesToFile File="FrameworkReferences.txt" Lines="@(ReferencePath)" Overwrite="true" WriteOnlyWhenDifferent="true" />
    </Target>
    
</Project>"""

    let mutable output = ""
    let mutable errors = ""
    let mutable cleanUp = true
    let projectDirectory = Path.Combine(Path.GetTempPath(), "FSharpBenchmarks", Path.GetRandomFileName())
    try
        try
            Directory.CreateDirectory(projectDirectory) |> ignore
            let projectFileName = Path.Combine(projectDirectory, "ProjectFile.fsproj")
            let programFsFileName = Path.Combine(projectDirectory, "Program.fs")
            let frameworkReferencesFileName = Path.Combine(projectDirectory, "FrameworkReferences.txt")
            File.WriteAllText(projectFileName, projectFile)
            File.WriteAllText(programFsFileName, programFs)

            let pInfo = ProcessStartInfo ()
            pInfo.FileName <- "dotnet"
            pInfo.Arguments <- "build"
            pInfo.WorkingDirectory <- projectDirectory
            pInfo.RedirectStandardOutput <- true
            pInfo.RedirectStandardError <- true
            pInfo.UseShellExecute <- false

            let p = Process.Start(pInfo)
            let timeout = 30000
            let succeeded = p.WaitForExit(timeout)

            output <- p.StandardOutput.ReadToEnd ()
            errors <- p.StandardError.ReadToEnd ()

            if not (String.IsNullOrWhiteSpace errors) then failwithf "%A" errors
            if p.ExitCode <> 0 then failwithf "Program exited with exit code %d" p.ExitCode
            if not succeeded then failwithf "Program timed out after %d ms" timeout

            File.ReadLines(frameworkReferencesFileName) |> Seq.toArray
        with | e ->
            cleanUp <- false
            printfn "Project directory: %s" projectDirectory
            printfn "STDOUT: %s" output
            printfn "STDERR: %s" errors
            raise (new Exception (sprintf "An error occurred getting netcoreapp references: %A" e))
    finally
        if cleanUp then
            try Directory.Delete(projectDirectory) with | _ -> ()