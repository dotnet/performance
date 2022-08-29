using System;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ILLinkBenchmarks;

[BenchmarkCategory("ILLink")]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, targetCount: 10)]
public class MSBuildBenchmark
{
    string projectFilePath;
    string _LinkSemaphore;

    [GlobalSetup(Targets = new[] {
             nameof(LinkHelloWorld) })]
    public void BuildHelloWorld()
    {
        projectFilePath = Environment.GetEnvironmentVariable("ILLINK_SAMPLE_PROJECT");
        _LinkSemaphore = Path.Combine(Path.GetDirectoryName(projectFilePath), "link.semaphore");
        // var X = new Project(projectFilePath);
        // if (X.Targets.TryGetValue("publish", out ProjectTargetInstance illinkTarget)) {
        // }
        // var project = X.CreateProjectInstance(ProjectInstanceSettings.ImmutableWithFastItemLookup);
        var p = Process.Start("dotnet", $"publish {projectFilePath} --use-current-runtime");
        p.WaitForExit(-1);
        return;
    }

    [Benchmark]
    [BenchmarkCategory("ILLink")]
    public bool LinkHelloWorld()
    {

        var p = Process.Start("dotnet", $"publish {projectFilePath} --use-current-runtime --no-build /p:PublishTrimmed=true /p:_LinkSemaphore={_LinkSemaphore} -bl");
        var finished = p.WaitForExit(-1);
        File.Delete(_LinkSemaphore);
        return finished;
    }
}
//     [BenchmarkCategory("ILLink")]
//     public class MSBuildBenchmark
//     {
//         string _hostPath;
//         string _assemblyPath;
//         string _frameworkFolder;
//         TaskItem[] assemblyPaths;
//         TaskItem[] referenceAssemblyPaths;

//         TaskItem rootAssembly;
//         TaskItem trimMode;
//         TaskItem outputDirectory;
//         TaskItem[] rootAssemblyNames;

//         [GlobalSetup(Targets = new[] {
//              nameof(ILLinkHelloWorld) })]
//         public System.Threading.Tasks.Task PrepTask()
//         {
//             Console.WriteLine(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH"));
//             _hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
//             _assemblyPath = @"C:\Users\jschuster\source\repro\link6\obj\Debug\net6.0\win-x64\link6.dll";
//             _frameworkFolder = @"C:\Users\jschuster\.nuget\packages\microsoft.netcore.app.runtime.win-x64\6.0.8\runtimes\win-x64\lib\net6.0\";

//             assemblyPaths = System.IO.Directory.EnumerateFiles(_frameworkFolder, "*.dll").Select(a => new TaskItem(a)).Append(new TaskItem(_assemblyPath)).ToArray();
//             referenceAssemblyPaths = System.IO.Directory.EnumerateFiles(_frameworkFolder, "*.dll").Select(a => new TaskItem(a)).Append(new TaskItem(_assemblyPath)).ToArray();
//             rootAssembly = new TaskItem(_assemblyPath);
//             trimMode = new TaskItem("copy");
//             outputDirectory = new TaskItem("linked");
//             rootAssemblyNames = new TaskItem[] { new TaskItem(_assemblyPath) };

//             return System.Threading.Tasks.Task.CompletedTask;
//         }

//         [Benchmark]
//         [BenchmarkCategory("ILLink")]
//         public bool ILLinkHelloWorld()
//         {
//             var x = new ILLink.Tasks.ILLink
//             {
//                 TreatWarningsAsErrors = false,
//                 Warn = "5",
//                 TrimMode = "copy",
//                 AssemblyPaths = assemblyPaths,
//                 RemoveSymbols = false,
//                 OutputDirectory = outputDirectory,
//                 DefaultAction = "copy",
//                 ReferenceAssemblyPaths = referenceAssemblyPaths,
//                 WarningsAsErrors = ";NU1605",
//                 RootAssemblyNames = rootAssemblyNames,
//                 NoWarn = "1701;1702;IL2121;1701;1702",
//                 SingleWarn = false,
//                 ExtraArgs = "--enable-serialization-discovery --skip-unresolved true",
//                 EnvironmentVariables = new string[] { $"DOTNET_HOST_PATH={_hostPath}" }
//             };
//             return x.Execute();
//         }
//     }
