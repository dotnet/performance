# BenchmarkDotNet

BenchmarkDotNet is the benchmarking tool that allows to run benchmarks for .NET, .NET Core, CoreRT and Mono. Including private runtime builds.

## Table of Contents

- [BenchmarkDotNet](#benchmarkdotnet)
  - [Table of Contents](#table-of-contents)
  - [Main Concepts](#main-concepts)
  - [Prerequisites](#prerequisites)
  - [Building the benchmarks](#building-the-benchmarks)
    - [Using .NET Cli](#using-net-cli)
    - [Using Python script](#using-python-script)
  - [Running the Benchmarks](#running-the-benchmarks)
    - [Interactive Mode](#interactive-mode)
    - [Command Line](#command-line)
      - [Filtering the Benchmarks](#filtering-the-benchmarks)
      - [Listing the Benchmarks](#listing-the-benchmarks)
  - [Reading the Results](#reading-the-results)
    - [Reading the Histogram](#reading-the-histogram)
    - [Reading Memory Statistics](#reading-memory-statistics)
  - [Profiling](#profiling)
  - [Disassembly](#disassembly)
  - [Multiple Runtimes](#multiple-runtimes)
  - [Regressions](#regressions)
  - [Private Runtime Builds](#private-runtime-builds)
    - [Running In Process](#running-in-process)
    - [CoreRun](#corerun)
    - [dotnet cli](#dotnet-cli)
    - [Private CLR Build](#private-clr-build)
    - [Private CoreRT Build](#private-corert-build)

## Main Concepts

Benchmarking is really hard (especially microbenchmarking), you can easily make a mistake during performance measurements.
BenchmarkDotNet will protect you from the common pitfalls (even for experienced developers) because it does all the dirty work for you:

- it generates an isolated project per runtime (with boilerplate code)
- it builds the project in `Release` (using Roslyn or dotnet cli)
- it runs every benchmark in a stand-alone process (to achieve process isolation and avoid side effects)
- it estimates the perfect invocation count per iteration (based on `IterationTime`)
- it warms-up the code
- it evaluates the overhead
- it runs multiple iterations of the method until the requested level of precision is met
- it consumes the benchmark result to avoid dead code elimination
- it prevents from inlining of the benchmark by wrapping it with a delegate
- it prints the results to the console in GitHub markdown, so you can just copy-paste the printed table to GitHub
- it exports the results to `BenchmarkDotNet.Artifacts\results` so you can store them for later use.

A few useful links for you:

- If you want to know more about BenchmarkDotNet features, check out the [Overview Page](http://benchmarkdotnet.org/Overview.htm).
- If you want to use BenchmarkDotNet for the first time, the [Getting Started](http://benchmarkdotnet.org/GettingStarted.htm) will help you.
- If you want to ask a quick question or discuss performance topics, use the [gitter](https://gitter.im/dotnet/BenchmarkDotNet) channel.

## Prerequisites

In order to build or run the benchmarks you will need the **.NET Core command-line interface (CLI) tools**. For more information please refer to the [prerequisites](./prerequisites.md).

## Building the benchmarks

### Using .NET Cli

To build the benchmarks you need to have the right `dotnet cli`. This repository allows you to benchmark .NET Core 3.1, .NET 6.0, .NET 7.0, and .NET 8.0 so you need to install all of them.

All you need to do is run the following command:

```cmd
dotnet build -c Release
```

If you don't want to install all of them and just run the benchmarks for selected runtime(s), you need to manually edit the [MicroBenchmarks.csproj](../src/benchmarks/micro/MicroBenchmarks.csproj) file.

```diff
-<TargetFrameworks>netcoreapp3.1;net6.0;net7.0;net8.0;net9.0</TargetFrameworks>
+<TargetFrameworks>net9.0</TargetFrameworks>
```

The alternative is to set `PERFLAB_TARGET_FRAMEWORKS` environment variable to selected Target Framework Moniker.

### Using Python script

If you don't want to install `dotnet cli` manually, we have a Python 3 script which can do that for you. All you need to do is to provide the frameworks:

```cmd
py .\scripts\benchmarks_ci.py --frameworks net9.0
```

## Running the Benchmarks

### Interactive Mode

To run the benchmarks in interactive mode you have to execute `dotnet run -c Release -f $targetFrameworkMoniker` in the folder with benchmarks project.

```cmd
C:\Projects\performance\src\benchmarks\micro> dotnet run -c Release -f net9.0
Available Benchmarks:
  #0   Burgers
  #1   ByteMark
  #2   CscBench
  #3   LinqBenchmarks
  #4   SeekUnroll
  #5   Binary_FromStream<LoginViewModel>
  #6   Binary_FromStream<Location>
  #7   Binary_FromStream<IndexViewModel>
  #8   Binary_FromStream<MyEventsListerViewModel>
  #9   Binary_FromStream<CollectionsOfPrimitives>
  #10  Binary_ToStream<LoginViewModel>
  ..... // the list continues

You should select the target benchmark(s). Please, print a number of a benchmark (e.g. '0') or a contained benchmark caption (e.g. 'Burgers'):
```

And select one of the benchmarks from the list by either entering its number or name.

### Command Line

#### Filtering the Benchmarks

You can filter the benchmarks using `--filter $globPattern` console line argument. The filter is **case insensitive**.

The glob patterns are applied to full benchmark name: namespace.typeName.methodName. Examples (all in the `src\benchmarks\micro` folder):

- Run all the benchmarks from BenchmarksGame namespace:

```cmd
dotnet run -c Release -f net9.0 --filter BenchmarksGame*
```

- Run all the benchmarks with type name Richards:

```cmd
dotnet run -c Release -f net9.0 --filter *.Richards.*
```

- Run all the benchmarks with method name ToStream:

```cmd
dotnet run -c Release -f net9.0 --filter *.ToStream
```

- Run ALL benchmarks:

```cmd
dotnet run -c Release -f net9.0 --filter *
```

- You can provide many filters (logical disjunction):

```cmd
dotnet run -c Release -f net9.0 --filter System.Collections*.Dictionary* *.Perf_Dictionary.*
```

- To print a **joined summary** for all of the benchmarks (by default printed per type), use `--join`:

```cmd
dotnet run -c Release -f net9.0 --filter BenchmarksGame* --join
```

Please remember that on **Unix** systems `*` is resolved to all files in current directory, so you need to escape it `'*'`.

#### Listing the Benchmarks

To print the list of all available benchmarks you need to pass `--list [tree/flat]` argument. It can also be combined with `--filter` option.

Example: Show the tree of all the benchmarks from System.Threading namespace that can be run for .NET 7.0:

```cmd
dotnet run -c Release -f net9.0 --list tree --filter System.Threading*
```

```log
System
 └─Threading
    ├─Channels
    │  └─Tests
    │     ├─BoundedChannelPerfTests
    │     │  ├─TryWriteThenTryRead
    │     │  ├─WriteAsyncThenReadAsync
    │     │  ├─ReadAsyncThenWriteAsync
    │     │  └─PingPong
    │     ├─SpscUnboundedChannelPerfTests
    │     │  ├─TryWriteThenTryRead
    │     │  ├─WriteAsyncThenReadAsync
    │     │  ├─ReadAsyncThenWriteAsync
    │     │  └─PingPong
    │     └─UnboundedChannelPerfTests
    │        ├─TryWriteThenTryRead
    │        ├─WriteAsyncThenReadAsync
    │        ├─ReadAsyncThenWriteAsync
    │        └─PingPong
    ├─Tasks
    │  ├─ValueTaskPerfTest
    │  │  ├─Await_FromResult
  ..... // the list continues
```

## Reading the Results

BenchmarkDotNet prints all important statistics to the console:

```log
Mean = 429.6484 ns, StdErr = 2.8826 ns (0.67%); N = 18, StdDev = 12.2296 ns
Min = 419.6885 ns, Q1 = 422.2286 ns, Median = 425.6508 ns, Q3 = 428.5053 ns, Max = 459.5134 ns
IQR = 6.2768 ns, LowerFence = 412.8134 ns, UpperFence = 437.9205 ns
ConfidenceInterval = [418.2188 ns; 441.0781 ns] (CI 99.9%), Margin = 11.4297 ns (2.66% of Mean)
Skewness = 1.46, Kurtosis = 3.57, MValue = 2
```

Due to the limited space in the console, only Mean, Standard Error and Standard Deviation are printed in the table:

| Method |     Mean |    Error |   StdDev |
|------- |---------:|---------:|---------:|
|  Adams | 429.6 ns | 11.43 ns | 12.23 ns |

### Reading the Histogram

Each Iteration is represented by `@`:

```log
-------------------- Histogram --------------------
[415.605 ns ; 429.290 ns) | @@@@@@@@@@@@@@@
[429.290 ns ; 445.623 ns) |
[445.623 ns ; 460.728 ns) | @@@
---------------------------------------------------
```

### Reading Memory Statistics

The results include managed memory statistics from [Memory Diagnoser](http://adamsitnik.com/the-new-Memory-Diagnoser/)

| Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------:|------------:|------------:|--------------------:|
|      0.0087 |           - |           - |                64 B |

- Allocated contains the size of the allocated **managed** memory. **Stackalloc/native heap allocations are not -included.** It's per single invocation, **inclusive**.
- **For .NET Core 3.0 preview6+ the Allocated Memory is for all threads that were live during the benchmark execution. Before .NET Core 3.0 preview6+ the Allocated Memory is only for the current thread**.
- The `Gen X/1k Op` column contains the number of `Gen X` collections per ***1 000*** Operations. If the value is- equal 1, then it means that GC collects memory once per one thousand of benchmark invocations in generation -`X`. BenchmarkDotNet is using some heuristic when running benchmarks, so the number of invocations can be -different for different runs. Scaling makes the results comparable.
- `-` in the Gen column means that no garbage collection was performed.

## Profiling

If you want to profile the benchmarked code, you can use some of the built-in profilers offered by BenchmarkDotNet:

- [ETW Profiler](https://adamsitnik.com/ETW-Profiler/) - it profiles the benchmarked .NET code on Windows and exports the data to a trace file which can be opened with PerfView or Windows Performance Analyzer.
- [Concurrency Visualizer Profile](https://adamsitnik.com/ConcurrencyVisualizer-Profiler/) - it profiles the benchmarked .NET code on Windows and exports the data to a trace file which can be opened with Concurrency Visualizer. The exported Trace file can be also opened with PerfView or Windows Performance Analyzer

Commands that enable the profilers are: `--profiler ETW` and `--profiler CV`.

After running the benchmarks, BenchmarkDotNet is going to print the path to the trace files:

```log
// * Diagnostic Output - EtwProfiler *
Exported 1 trace file(s). Example:
C:\Projects\performance\artifacts\20190215-0303-51368\Benchstone\BenchF\Adams\Test.etl
```

## Disassembly

If you want to disassemble the benchmarked code, you need to use the [Disassembly Diagnoser](http://adamsitnik.com/Disassembly-Diagnoser/). It allows disassembling `asm/C#/IL` in recursive way on Windows for .NET and .NET Core (all Jits) and `asm` for Mono on any OS.

You can do that by passing `--disassm` to the app or by using `[DisassemblyDiagnoser(printAsm: true, printSource: true)]` attribute or by adding it to your config with `config.With(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(printAsm: true, recursiveDepth: 1))`.

Example: `dotnet run -c Release -f net9.0 -- --filter System.Memory.Span<Int32>.Reverse -d`

```assembly
; System.Runtime.InteropServices.MemoryMarshal.GetReference[[System.Byte, System.Private.CoreLib]](System.Span`1<Byte>)
       sub     rsp,28h
       cmp     qword ptr [rcx],0
       jne     M00_L00
       mov     rcx,qword ptr [rcx+8]
       call    System.Runtime.CompilerServices.Unsafe.AsRef[[System.Byte, System.Private.CoreLib]](Void*)
       nop
       add     rsp,28h
       ret
M00_L00:
       mov     rax,qword ptr [rcx]
       cmp     dword ptr [rax],eax
       add     rax,8
       mov     rdx,qword ptr [rcx+8]
       add     rax,rdx
       add     rsp,28h
       ret
```

## Multiple Runtimes

The `--runtimes` or just `-r` allows you to run the benchmarks for **multiple Runtimes**.

Available options are: Mono, wasmnet70, CoreRT, net462, net47, net471, net472, netcoreapp3.1, net6.0, net7.0, net8.0, and net9.0.

Example: run the benchmarks for .NET 7.0 and 8.0:

```cmd
dotnet run -c Release -f net7.0 --runtimes net7.0 net8.0
```

**Important: The host process needs to be the lowest common API denominator of the runtimes you want to compare!** In this case, it was `net7.0`.

## Regressions

To perform a Mann–Whitney U Test and display the results in a dedicated column you need to provide the Threshold for Statistical Test via `--statisticalTest` argument. The value can be relative (5%) or absolute (10ms, 100ns, 1s)

Example: run Mann–Whitney U test with relative ratio of 5% for `BinaryTrees_2` for .NET 7.0 (base) vs .NET 8.0 (diff). .NET 7.0 will be baseline because it was first.

```cmd
dotnet run -c Release -f net8.0 --filter *BinaryTrees_2* --runtimes net7.0 net8.0 --statisticalTest 5%
```

|        Method |     Toolchain |     Mean | MannWhitney(5%) |
|-------------- |-------------- |---------:|---------------- |
| BinaryTrees_2 |        net7.0 | 124.4 ms |            Base |
| BinaryTrees_2 |        net6.0 | 153.7 ms |          Slower |

**Note:** to compare the historical results you need to use [Results Comparer](../src/tools/ResultsComparer/README.md)

## Private Runtime Builds

When you run the BenchmarkDotNet benchmarks using dotnet cli it runs them against selected .NET Runtime using the SDK from PATH. If you want to run the benchmarks against local build of .NET/.NET Core/CoreRT/Mono you need to make it explicit.

### Running In Process

Sometimes the easiest way to run the benchmarks against the local build of any non-AOT .NET Runtime is to copy all the files into one place, build the app with `csc` and just run the executable.

If you want to run the benchmarks in the same process, without creating a dedicated executable and process-level isolation, please use `--inProcess` (or just `-i`).

Please use this option only when you are sure that the benchmarks you want to run **have no side effects**. Allocating managed memory has side effects!

### CoreRun

It's possible to benchmark private builds of [dotnet/runtime](https://github.com/dotnet/runtime) using CoreRun.

```cmd
dotnet run -c Release -f net9.0 --coreRun $thePath
```

**Note:** You can provide more than 1 path to CoreRun. In such case, the first path will be the baseline and all the benchmarks are going to be executed for all CoreRuns you have specified.

**Note:** If `CoreRunToolchain` detects that you have some older version of dependencies required to run the benchmarks in CoreRun folder, it's going to overwrite them with newer versions from the published app. It's going to do that in a shadow copy of the folder with CoreRun, so your configuration remains untouched.

If you are not sure which assemblies are loaded and used you can use the following code to find out:

```cs
[GlobalSetup]
public void PrintInfo()
{
    var systemPrivateCoreLib = FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
    Console.WriteLine($"// System.Private.CoreLib version {systemPrivateCoreLib.FileVersion}, location {typeof(object).Assembly.Location}, product version {systemPrivateCoreLib.ProductVersion}");
}
```

### dotnet cli

You can also use any dotnet cli to build and run the benchmarks.

```cmd
dotnet run -c Release -f net9.0 --cli "C:\Projects\performance\.dotnet\dotnet.exe"
```

This is very useful when you want to compare different builds of .NET.

### Private CLR Build

It's possible to benchmark a private build of .NET Runtime. You just need to pass the value of `COMPLUS_Version` to BenchmarkDotNet. You can do that by either using `--clrVersion $theVersion` as an argument or `Job.ShortRun.With(new ClrRuntime(version: "$theVersion"))` in the code.

So if you made a change in CLR and want to measure the difference, you can run the benchmarks with:

```cmd
dotnet run -c Release -f net48 -- --clrVersion $theVersion
```

More info can be found [here](https://github.com/dotnet/BenchmarkDotNet/issues/706).

### Private CoreRT Build

To run benchmarks with private CoreRT build you need to provide the `IlcPath`. Example:

```cmd
dotnet run -c Release -f net9.0 -- --ilcPath C:\Projects\corert\bin\Windows_NT.x64.Release
```
