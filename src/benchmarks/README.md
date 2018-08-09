# Benchmarks

This repo contains various .NET benchmarks. It uses BenchmarkDotNet as the benchmarking engine to run benchmarks for .NET, .NET Core, CoreRT and Mono. Including private runtime builds.

## BenchmarkDotNet

Benchmarking is really hard (especially microbenchmarking), you can easily make a mistake during performance measurements.
BenchmarkDotNet will protect you from the common pitfalls (even for experienced developers) because it does all the dirty work for you:

* it generates an isolated project per runtime
* it builds the project in `Release`
* it runs every benchmark in a stand-alone process (to achieve process isolation and avoid side effects)
* it estimates the perfect invocation count per iteration (based on `IterationTime`)
* it warms-up the code
* it evaluates the overhead
* it runs multiple iterations of the method until the requested level of precision is met
* it consumes the benchmark result to avoid dead code elimination
* it prevents from inlining of the benchmark by wrapping it with a delegate.

A few useful links for you:

* If you want to know more about BenchmarkDotNet features, check out the [Overview Page](http://benchmarkdotnet.org/Overview.htm).
* If you want to use BenchmarkDotNet for the first time, the [Getting Started](http://benchmarkdotnet.org/GettingStarted.htm) will help you.
* If you want to ask a quick question or discuss performance topics, use the [gitter](https://gitter.im/dotnet/BenchmarkDotNet) channel.

## Your first benchmark

It's really easy to design a performance experiment with BenchmarkDotNet. Just mark your method with the `[Benchmark]` attribute and the benchmark is ready.

```cs
public class Simple
{
    [Benchmark]
    public byte[] CreateByteArray() => new byte[8];
}
```

Any public, non-generic type with public `[Benchmark]` method in this assembly will be auto-detected and added to the benchmarks list.

## Running

To run the benchmarks you have to execute `dotnet run -c Release -f net46|netcoreapp2.0|netcoreapp2.1` (choose one of the supported frameworks).

![Choose Benchmark](./img/chooseBenchmark.png)

And select one of the benchmarks from the list by either entering it's number or name. To **run all** the benchmarks simply enter `*` to the console.

BenchmarkDotNet will build the executables, run the benchmarks, print the results to console and **export the results** to `.\BenchmarkDotNet.Artifacts\results`. 

![Exported results](./img/exportedResults.png)

BenchmarkDotNet by default exports the results to GitHub markdown, so you can just find the right `.md` file in `results` folder and copy-paste the markdown to GitHub.

## Filtering

You can filter the benchmarks by namespace, category, type name and method name. Examples:

* `dotnet run -c Release -f netcoreapp2.1 -- --categories CoreCLR Span` - will run all the benchmarks that belong to CoreCLR **AND** Span category
* `dotnet run -c Release -f netcoreapp2.1 -- --anyCategories CoreCLR CoreFX` - will run all the benchmarks that belong to CoreCLR **OR** CoreFX category
* `dotnet run -c Release -f netcoreapp2.1 -- --filter BenchmarksGame*` - will run all the benchmarks from BenchmarksGame namespace
* `dotnet run -c Release -f netcoreapp2.1 -- --filter *.ToStream` - will run all the benchmarks with method name ToStream
* `dotnet run -c Release -f netcoreapp2.1 -- --filter *.Richards.*` - will run all the benchmarks with type name Richards

**Note:** To print a single summary for all of the benchmarks, use `--join`. 
Example: `dotnet run -c Release -f netcoreapp2.1 -- --join -f BenchmarksGame*` - will run all of the benchmarks from BenchmarksGame namespace and print a single summary.

## All Statistics

By default BenchmarkDotNet displays only `Mean`, `Error` and `StdDev` in the results. If you want to see more statistics, please pass `--allStats` as an extra argument to the app: `dotnet run -c Release -f netcoreapp2.1 -- --allStats`. If you build your own config, please use `config.With(StatisticColumn.AllStatistics)`.

|   Method |     Mean |     Error |    StdDev |    StdErr |      Min |       Q1 |   Median |       Q3 |      Max |        Op/s |  Gen 0 | Allocated |
|--------- |---------:|----------:|----------:|----------:|---------:|---------:|---------:|---------:|---------:|------------:|-------:|----------:|
|      Jil | 458.2 ns |  38.63 ns |  2.183 ns |  1.260 ns | 455.8 ns | 455.8 ns | 458.9 ns | 460.0 ns | 460.0 ns | 2,182,387.2 | 0.1163 |     736 B |
| JSON.NET | 869.8 ns |  47.37 ns |  2.677 ns |  1.545 ns | 867.7 ns | 867.7 ns | 868.8 ns | 872.8 ns | 872.8 ns | 1,149,736.0 | 0.2394 |    1512 B |
| Utf8Json | 272.6 ns | 341.64 ns | 19.303 ns | 11.145 ns | 256.7 ns | 256.7 ns | 266.9 ns | 294.1 ns | 294.1 ns | 3,668,854.8 | 0.0300 |     192 B |

## How to read the Memory Statistics

The project is configured to include managed memory statistics by using [Memory Diagnoser](http://adamsitnik.com/the-new-Memory-Diagnoser/)

|     Method |  Gen 0 | Allocated |
|----------- |------- |---------- |
|          A |      - |       0 B |
|          B |      1 |     496 B |

* Allocated contains the size of allocated **managed** memory. **Stackalloc/native heap allocations are not included.** It's per single invocation, **inclusive**.
* The `Gen X` column contains the number of `Gen X` collections per ***1 000*** Operations. If the value is equal 1, then it means that GC collects memory once per one thousand of benchmark invocations in generation `X`. BenchmarkDotNet is using some heuristic when running benchmarks, so the number of invocations can be different for different runs. Scaling makes the results comparable.
* `-` in the Gen column means that no garbage collection was performed.
* If `Gen X` column is not present, then it means that no garbage collection was performed for generation `X`. If none of your benchmarks induces the GC, the Gen columns are not present.

## How to get the Disassembly

If you want to disassemble the benchmarked code, you need to use the [Disassembly Diagnoser](http://adamsitnik.com/Disassembly-Diagnoser/). It allows to disassemble `asm/C#/IL` in recursive way on Windows for .NET and .NET Core (all Jits) and `asm` for Mono on any OS.

You can do that by passing `--disassm` to the app or by using `[DisassemblyDiagnoser(printAsm: true, printSource: true)]` attribute or by adding it to your config with `config.With(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(printAsm: true, recursiveDepth: 1))`. 

![Sample Disassm](./img/sampleDisassm.png)

## How to run In Process

If you want to run the benchmarks in process, without creating a dedicated executable and process-level isolation, please pass `--inProcess` as an extra argument to the app: `dotnet run -c Release -f netcoreapp2.1 -- --inProcess`. If you build your own config, please use `config.With(Job.Default.With(InProcessToolchain.Instance))`. Please use this option only when you are sure that the benchmarks you want to run have no side effects.

## How to compare different Runtimes

BenchmarkDotNet allows you to run benchmarks for multiple runtimes. By using this feature you can compare .NET vs .NET Core vs CoreRT vs Mono or .NET Core 2.0 vs .NET Core 2.1. BDN will compile and run the right stuff for you.

* for .NET pass `--clr` to the app or use `Job.Default.With(Runtime.Clr)` in the code.
* for .NET Core 2.0 pass `--core20` to the app or use `Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp20)` in the code.
* for .NET Core 2.1 pass `--core21` to the app or use `Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp20)` in the code.
* for the latest CoreRT pass `--coreRt` to the app or use `Job.Default.With(Runtime.CoreRT).With(CoreRtToolchain.LatestMyGetBuild)` in the code. **Be warned!** Downloading latest CoreRT with all the dependencies takes a lot of time. It is recommended to choose one version and use it for comparisions, more info [here](https://github.com/dotnet/BenchmarkDotNet/blob/600e5fa81bd8e7a1d32a60b2bea830e1f46106eb/docs/guide/Configs/Toolchains.md#corert). To use explicit CoreRT version please use `coreRtVersion` argument. Example: `dotnet run -c Release -f netcoreapp2.1 --coreRtVersion 1.0.0-alpha-26414-0`
* for Mono pass `--mono` to the app or use `Job.Default.With(Runtime.Mono)` in the code.

An example command for comparing 4 runtimes: `dotnet run -c Release -f netcoreapp2.1 -- --core20 --core21 --mono --clr --coreRt`

``` ini
BenchmarkDotNet=v0.10.14.516-nightly, OS=Windows 10.0.16299.309 (1709/FallCreatorsUpdate/Redstone3)
Intel Xeon CPU E5-1650 v4 3.60GHz, 1 CPU, 12 logical and 6 physical cores
Frequency=3507504 Hz, Resolution=285.1030 ns, Timer=TSC
.NET Core SDK=2.1.300-preview1-008174
  [Host]     : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  Job-GALXOG : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2633.0
  Job-DRRTOZ : .NET Core 2.0.6 (CoreCLR 4.6.26212.01, CoreFX 4.6.26212.01), 64bit RyuJIT
  Job-QQFGIW : .NET Core 2.1.0-preview1-26216-03 (CoreCLR 4.6.26216.04, CoreFX 4.6.26216.02), 64bit RyuJIT
  Job-GKRDGF : .NET CoreRT 1.0.26412.02, 64bit AOT
  Job-HNFRHF : Mono 5.10.0 (Visual Studio), 64bit 

LaunchCount=1  TargetCount=3  WarmupCount=3  
```

|   Method | Runtime |                    Toolchain |      Mean |      Error |    StdDev | Allocated |
|--------- |-------- |----------------------------- |----------:|-----------:|----------:|----------:|
| ParseInt |     Clr |                      Default |  95.95 ns |   5.354 ns | 0.3025 ns |       0 B |
| ParseInt |    Core |                .NET Core 2.0 | 104.71 ns | 121.620 ns | 6.8718 ns |       0 B |
| ParseInt |    Core |                .NET Core 2.1 |  93.16 ns |   6.383 ns | 0.3606 ns |       0 B |
| ParseInt |  CoreRT | Core RT 1.0.0-alpha-26412-02 | 110.02 ns |  71.947 ns | 4.0651 ns |       0 B |
| ParseInt |    Mono |                      Default | 133.19 ns | 133.928 ns | 7.5672 ns |       N/A |

## .NET Core 2.0 vs .NET Core 2.1

If you want to compare .NET Core 2.0 vs .NET Core 2.1 you can just pass `-- --core20 --core21`. You can also build a custom config and mark selected runtime as baseline, then all the results will be scaled to the baseline.

```cs
Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp20)).WithId("Core 2.0").AsBaseline());
Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21)).WithId("Core 2.1"));
```

|                Method |      Job |     Toolchain | IsBaseline |     Mean |     Error |    StdDev | Scaled |
|---------------------- |--------- |-------------- |----------- |---------:|----------:|----------:|-------:|
| CompactLoopBodyLayout | Core 2.0 | .NET Core 2.0 |       True | 36.72 ns | 0.1583 ns | 0.1481 ns |   1.00 |
| CompactLoopBodyLayout | Core 2.1 | .NET Core 2.1 |    Default | 30.47 ns | 0.1731 ns | 0.1619 ns |   0.83 |

## Benchmarking private CoreCLR build using CoreRun

It's possible to benchmark a private build of CoreCLR using CoreRun. You just need to pass the path to CoreRun to BenchmarkDotNet. You can do that by either using `--coreRun $thePath` as an arugment or `job.With(new CoreRunToolchain(coreRunPath: "$thePath"))` in the code.

So if you made a change in CoreCLR and want to measure the difference with .NET Core 2.1, you can run the benchmarks with `dotnet run -c Release -f netcoreapp2.1 -- --core21 --coreRun $thePath`.

**Note:** If `CoreRunToolchain` detects that you have some older version of dependencies required to run the benchmarks in CoreRun folder, it's going to overwrite them with newer versions from the published app. It's going to do that in a shadow copy of the folder with CorRun, so your configuration remains untouched.

If you are not sure which assemblies gets loaded and used you can use following code to find out:

```cs
[GlobalSetup]
public void PrintInfo()
{
	var coreFxAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(Regex).GetTypeInfo().Assembly.Location);
	var coreClrAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).GetTypeInfo().Assembly.Location);

	Console.WriteLine($"// CoreFx version: {coreFxAssemblyInfo.FileVersion}, location {typeof(Regex).GetTypeInfo().Assembly.Location}, product version {coreFxAssemblyInfo.ProductVersion}");
	Console.WriteLine($"// CoreClr version {coreClrAssemblyInfo.FileVersion}, location {typeof(object).GetTypeInfo().Assembly.Location}, product version {coreClrAssemblyInfo.ProductVersion}");
}
```

## Benchmarking private CLR build

It's possible to benchmark a private build of .NET Runtime. You just need to pass the value of `COMPLUS_Version` to BenchmarkDotNet. You can do that by either using `--clrVersion $theVersion` as an arugment or `Job.ShortRun.With(new ClrRuntime(version: "$theVersiong"))` in the code.

So if you made a change in CLR and want to measure the difference, you can run the benchmarks with `dotnet run -c Release -f net46 -- --clr --clrVersion $theVersion`. More info can be found [here](https://github.com/dotnet/BenchmarkDotNet/issues/706).

## Any CoreCLR and CoreFX

BenchmarkDotNet allows the users to run their benchmarks against ANY CoreCLR and CoreFX builds. You can compare your local build vs MyGet feed or Debug vs Release or one version vs another.

To avoid problems described [here](https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md#update-coreclr-using-runtime-nuget-package) a temporary folder is used when restoring packages for local builds. This is why it takes 20-30s in total to build the benchmarks. 

Entire feature with many examples is described [here](https://github.com/dotnet/BenchmarkDotNet/blob/600e5fa81bd8e7a1d32a60b2bea830e1f46106eb/docs/guide/Configs/Toolchains.md#custom-coreclr-and-corefx).

### Benchmarking private CoreFX build

To run benchmarks with private CoreFX build you need to provide the version of `Microsoft.Private.CoreFx.NETCoreApp` and the path to folder with CoreFX NuGet packages.

Sample arguments: `dotnet run -c Release -f netcoreapp2.1 -- --coreFxBin C:\Projects\forks\corefx\bin\packages\Release --coreFxVersion 4.5.0-preview2-26307-0`

Sample config: 

```cs
Job.ShortRun.With(
    CustomCoreClrToolchain.CreateBuilder()
        .UseCoreFxLocalBuild("4.5.0-preview2-26313-0", @"C:\Projects\forks\corefx\bin\packages\Release")
        .UseCoreClrDefault()
        .AdditionalNuGetFeed("benchmarkdotnet ci", "https://ci.appveyor.com/nuget/benchmarkdotnet");
        .DisplayName("local corefx")
        .ToToolchain());
```

### Benchmarking private CoreCLR build

To run benchmarks with private CoreCLR build you need to provide the version of `Microsoft.NETCore.Runtime`, path to folder with CoreCLR NuGet packages and path to `coreclr\packages` folder.

Sample arguments: `dotnet run -c Release -f netcoreapp2.1 -- --coreClrBin C:\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg --coreClrPackages C:\Projects\coreclr\packages --coreClrVersion 2.1.0-preview2-26305-0`

Sample config: 

```cs
Job.ShortRun.With(
    CustomCoreClrToolchain.CreateBuilder()
        .UseCoreClrLocalBuild("2.1.0-preview2-26313-0", @"C:\Projects\forks\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg", @"C:\Projects\coreclr\packages")
        .UseCoreFxDefault()
        .AdditionalNuGetFeed("benchmarkdotnet ci", "https://ci.appveyor.com/nuget/benchmarkdotnet");
        .DisplayName("local builds")
        .ToToolchain());
```

## Benchmarking private CoreRT build

To run benchmarks with private CoreRT build you need to provide the `IlcPath`.

Sample arguments: `dotnet run -c Release -f netcoreapp2.1 -- --ilcPath C:\Projects\corert\bin\Windows_NT.x64.Release`

Sample config: 

```cs
var config = DefaultConfig.Instance
    .With(Job.ShortRun
        .With(Runtime.CoreRT)
        .With(CoreRtToolchain.CreateBuilder()
            .UseCoreRtLocal(@"C:\Projects\corert\bin\Windows_NT.x64.Release") // IlcPath
            .DisplayName("Core RT RyuJit")
            .ToToolchain()));
```

## Testing how Processor Affinity and Loop Alignment affect results

To run the benchmarks with specific Processor Affinity, you need to provide the processor mask as an argument called `--affinity`.

Example: `dotnet run -c Release -f netcoreapp2.1 -- --affinity=8`

To run same benchmarks with and without specific Processor Affinity you need to use `--testAffinity` and also provide the mask with `--affinity`

Example: `dotnet run -c Release -f netcoreapp2.1 -- -f *BinaryTrees_2* --affinity=8 --testAffinity`

|        Method |     Affinity |
|-------------- |------------- |
| BinaryTrees_2 | 000000001000 |
| BinaryTrees_2 | 111111111111 |

To test how loop alignment affects the results, you can use `--testAlignment` which is going to run the benchmarks with env var `COMPlus_JitAlignLoops` set to `0` and `1`

Example: `dotnet run -c Release -f netcoreapp2.1 -- -f *BinaryTrees_2* --testAlignment`

|        Method |    EnvironmentVariables |
|-------------- |------------------------ |
| BinaryTrees_2 | COMPlus_JitAlignLoops=0 |
| BinaryTrees_2 | COMPlus_JitAlignLoops=1 |

**Note:** You can combine `--testAlignment` with `--testAffinity` which will results in 4 different benchmark runs:

|        Method |     Affinity |    EnvironmentVariables |
|-------------- |------------- |------------------------ |
| BinaryTrees_2 | 000000001000 | COMPlus_JitAlignLoops=0 |
| BinaryTrees_2 | 000000001000 | COMPlus_JitAlignLoops=1 |
| BinaryTrees_2 | 111111111111 | COMPlus_JitAlignLoops=0 |
| BinaryTrees_2 | 111111111111 | COMPlus_JitAlignLoops=1 |

## Enabling given benchmark(s) for selected Operating System(s)

This is possible with the `AllowedOperatingSystemsAttribute`. You need to provide a mandatory comment and OS(es) which benchmark(s) can run on.

```cs
[AllowedOperatingSystems("Hangs on non-Windows, dotnet/corefx#18290", OS.Windows)]
public class Perf_PipeTest
```