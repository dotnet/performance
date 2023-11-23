# Micro Benchmarks

This folder contains micro benchmarks that test the performance of .NET Runtime(s).

## Tooling

To run the benchmarks, you need to download dotnet cli or use the python script, please see [Prerequisites](../../../docs/prerequisites.md) for more.

We use BenchmarkDotNet as the benchmarking tool, you can read more about it in [our short summary](../../../docs/benchmarkdotnet.md) (it's recommended). The key thing that you need to remember is that **BenchmarkDotNet runs every benchmark in a dedicated process and stops the benchmarking when a specified level of precision is met**.

To learn more about designing benchmarks, please read [Microbenchmark Design Guidelines](../../../docs/microbenchmark-design-guidelines.md). It's essential to be familar with these guidelines before modifying or adding any micro benchmarks.

## Quick Start

The first thing that you need to choose is the Target Framework. Available options are: `netcoreapp3.1|net6.0|net7.0|net8.0|net9.0|net462`. You can specify the target framework using `-f|--framework` argument. For the sake of simplicity, all examples below use `net9.0` as the target framework.

The following commands are run from the `src/benchmarks/micro` directory.

To run the benchmarks in Interactive Mode, where you will be asked which benchmark(s) to run:

```cmd
dotnet run -c Release -f net9.0
```

To list all available benchmarks ([read more](../../../docs/benchmarkdotnet.md#Listing-the-Benchmarks)):

```cmd
dotnet run -c Release -f net9.0 --list flat|tree
```

To filter the benchmarks using a glob pattern applied to namespace.typeName.methodName ([read more](../../../docs/benchmarkdotnet.md#Filtering-the-Benchmarks)):

```cmd
dotnet run -c Release -f net9.0 --filter *Span*
```

To profile the benchmarked code and produce an ETW Trace file ([read more](../../../docs/benchmarkdotnet.md#Profiling)):

```cmd
dotnet run -c Release -f net9.0 --filter $YourFilter --profiler ETW
```

To run the benchmarks for multiple runtimes ([read more](../../../docs/benchmarkdotnet.md#Multiple-Runtimes)):

```cmd
dotnet run -c Release -f net8.0 --filter * --runtimes net8.0 net9.0
```

## Private Runtime Builds

If you contribute to [dotnet/runtime](https://github.com/dotnet/runtime) and want to benchmark **local builds of .NET Core** you need to build [dotnet/runtime](https://github.com/dotnet/runtime) in Release (including tests - so a command similar to `build clr+libs+libs.tests -rc release -lc release`) and then provide the path(s) to CoreRun(s). Provided CoreRun(s) will be used to execute every benchmark in a dedicated process:

```cmd
dotnet run -c Release -f net9.0 --filter $YourFilter \
    --corerun C:\git\runtime\artifacts\bin\testhost\net9.0-windows-Release-x64\shared\Microsoft.NETCore.App\9.0.0\CoreRun.exe
```

To make sure that your changes don't introduce any regressions, you can provide paths to CoreRuns with and without your changes and use the Statistical Test feature to detect regressions/improvements ([read more](../../../docs/benchmarkdotnet.md#Regressions)):

```cmd
dotnet run -c Release -f net9.0 \
    --filter BenchmarksGame* \
    --statisticalTest 3ms \
    --coreRun \
        "C:\git\runtime_upstream\artifacts\bin\testhost\net9.0-windows-Release-x64\shared\Microsoft.NETCore.App\9.0.0\CoreRun.exe" \
        "C:\git\runtime_fork\artifacts\bin\testhost\net9.0-windows-Release-x64\shared\Microsoft.NETCore.App\9.0.0\CoreRun.exe"
```

If you **prefer to use dotnet cli** instead of CoreRun, you need to pass the path to cli via the `--cli` argument.

BenchmarkDotNet allows you to run the benchmarks for private builds of [Full .NET Framework](../../../docs/benchmarkdotnet.md#Private-CLR-Build) and [CoreRT](../../../docs/benchmarkdotnet.md#Private-CoreRT-Build)

We once again encourage you to read the [full docs about BenchmarkDotNet](../../../docs/benchmarkdotnet.md#table-of-contents).
