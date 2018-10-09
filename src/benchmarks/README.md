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

Any public, non-sealed type with public `[Benchmark]` method in this assembly will be auto-detected and added to the benchmarks list.

## Running

To run the benchmarks you have to execute `dotnet run -c Release -f net46|netcoreapp2.0|netcoreapp2.1|netcoreapp2.2|netcoreapp3.0` (choose one of the supported frameworks).

![Choose Benchmark](./img/chooseBenchmark.png)

And select one of the benchmarks from the list by either entering it's number or name. To **run all** the benchmarks simply enter `*` to the console.

BenchmarkDotNet will build the executables, run the benchmarks, print the results to console and **export the results** to `.\BenchmarkDotNet.Artifacts\results`. 

![Exported results](./img/exportedResults.png)

BenchmarkDotNet by default exports the results to GitHub markdown, so you can just find the right `.md` file in `results` folder and copy-paste the markdown to GitHub.

## Filtering

You can filter the benchmarks by namespace, category, type name and method name. Examples:

* `dotnet run -c Release -f netcoreapp2.1 -- --allCategories CoreCLR Span` - will run all the benchmarks that belong to CoreCLR **AND** Span category
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

## How to profile benchmarked code using ETW

If you want to profile the benchmarked code, you need to use the [ETW Profiler](https://adamsitnik.com/ETW-Profiler/). It allows to profile the benchmarked .NET code on Windows and exports the data to a trace file which can be opened with PerfView or Windows Performance Analyzer.

You can do that by passing `-p ETW` or `--profiler ETW` to the app. 

## How to run In Process

If you want to run the benchmarks in process, without creating a dedicated executable and process-level isolation, please pass `--inProcess` (or just `-i`) as an extra argument to the app: `dotnet run -c Release -f netcoreapp2.1 -- --inProcess`. If you build your own config, please use `config.With(Job.Default.With(InProcessToolchain.Instance))`. Please use this option only when you are sure that the benchmarks you want to run have no side effects.

## How to compare different Runtimes

The `--runtimes` or just `-r` allows you to run the benchmarks for selected Runtimes. Available options are: Mono, CoreRT, net46, net461, net462, net47, net471, net472, netcoreapp2.0, netcoreapp2.1, netcoreapp2.2, netcoreapp3.0.

Example: run the benchmarks for .NET 4.7.2 and .NET Core 2.1:

```log
dotnet run -c Release -- --runtimes net472 netcoreapp2.1
```

## Benchmarking private CoreCLR and CoreFX builds using CoreRun

It's possible to benchmark a private build of CoreCLR/FX using CoreRun. You just need to pass the path to CoreRun to BenchmarkDotNet. You can do that by either using `--coreRun $thePath` as an arugment or `job.With(new CoreRunToolchain(coreRunPath: "$thePath"))` in the code.

So if you made a change in CoreCLR/FX and want to measure the performance, you can run the benchmarks with `dotnet run -c Release -f netcoreapp3.0 -- --coreRun $thePath`.

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

## dotnet cli

You can also use any dotnet cli to build and run the benchmarks. To do that you need to pass the path to cli as an argument `--cli.`

Example: run the benchmarks for .NET Core 3.0 using dotnet cli from `C:\Projects\performance\.dotnet\dotnet.exe`:

```log
dotnet run -c Release -- -r netcoreapp3.0 --cli "C:\Projects\performance\.dotnet\dotnet.exe"
```

## Benchmarking private CLR build

It's possible to benchmark a private build of .NET Runtime. You just need to pass the value of `COMPLUS_Version` to BenchmarkDotNet. You can do that by either using `--clrVersion $theVersion` as an arugment or `Job.ShortRun.With(new ClrRuntime(version: "$theVersiong"))` in the code.

So if you made a change in CLR and want to measure the difference, you can run the benchmarks with `dotnet run -c Release -f net46 -- -r net472 --clrVersion $theVersion`. More info can be found [here](https://github.com/dotnet/BenchmarkDotNet/issues/706).

## Benchmarking private CoreRT build

To run benchmarks with private CoreRT build you need to provide the `IlcPath`.

Sample arguments: `dotnet run -c Release -f netcoreapp2.1 -- --ilcPath C:\Projects\corert\bin\Windows_NT.x64.Release`

## Enabling given benchmark(s) for selected Operating System(s)

This is possible with the `AllowedOperatingSystemsAttribute`. You need to provide a mandatory comment and OS(es) which benchmark(s) can run on.

```cs
[AllowedOperatingSystems("Hangs on non-Windows, dotnet/corefx#18290", OS.Windows)]
public class Perf_PipeTest
```