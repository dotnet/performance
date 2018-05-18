# CoreClr Code Quality Micro Benchmarks #

This is a collection of micro benchmarks, ported from [CoreClr](https://github.com/dotnet/coreclr.git), and originally designed to measure the .NET JIT performance, and they can be easily run for ad-hoc investigation.

## Supported target frameworks ##

- netcoreapp1.1
- netcoreapp2.0
- netcoreapp2.1

## About the harness ##

Currently, the benchmarks are harnessed with [xUnit Performance Api](https://github.com/Microsoft/xunit-performance.git)

## How to build (ad-hoc) ##

```cmd
dotnet restore PerformanceHarness/PerformanceHarness.csproj
dotnet publish PerformanceHarness/PerformanceHarness.csproj -c <Configuration> -f <Framework>
```

## Running the harness (ad-hoc) ##

### Example 1: Get help ###

Command:

```cmd
dotnet PerformanceHarness/bin/x64/Release/netcoreapp2.0/publish/PerformanceHarness.dll --help
```

Console output:

```log
Xunit-Performance-Api
Copyright (c) Microsoft Corporation 2015

  --perf:outputdir    Specifies the output directory name.
  --perf:runid        User defined id given to the performance harness.
  --perf:typenames    The (optional) type names of the test classes to run.
  --help              Display this help screen.
  --version           Display version information.
```

### Example 2: Running a single benchmark (Benchstone/BenchF/Adams) ###

Command:

```cmd
cd PerformanceHarness/bin/x64/Release/netcoreapp2.0/publish
dotnet PerformanceHarness.dll DotNetBenchmark-Adams.dll --perf:collect stopwatch+gcapi
```

Console output:

```log
[5/16/2018 5:35:42 PM][INF] Running 1 [Benchmark]s
[5/16/2018 5:35:43 PM][INF]   Benchstone.BenchF.Adams.Test
[5/16/2018 5:35:45 PM][INF] Finished 1 tests in 2.403s (0 failed, 0 skipped)
[5/16/2018 5:35:45 PM][INF] File saved to: "<repository root dir>\PerformanceHarness\bin\x64\Release\netcoreapp2.0\publish\20180517003542-Adams.etl"
[5/16/2018 5:35:45 PM][INF] File saved to: "<repository root dir>\PerformanceHarness\bin\x64\Release\netcoreapp2.0\publish\20180517003542-Adams.xml"
[5/16/2018 5:35:46 PM][INF] File saved to: "<repository root dir>\PerformanceHarness\bin\x64\Release\netcoreapp2.0\publish\20180517003542-Adams.md"
 Adams.dll                    | Metric                                        | Unit  | Iterations |    Average | STDEV.S |        Min |        Max
:---------------------------- |:--------------------------------------------- |:-----:|:----------:| ----------:| -------:| ----------:| ----------:
 Benchstone.BenchF.Adams.Test | Duration                                      | msec  |     21     |    103.089 |   2.344 |    101.353 |    112.778
 Benchstone.BenchF.Adams.Test | Allocation Size on Benchmark Execution Thread | bytes |     21     | 1.280E+007 |   0.000 | 1.280E+007 | 1.280E+007

[5/16/2018 5:35:46 PM][INF] File saved to: "<repository root dir>\PerformanceHarness\bin\x64\Release\netcoreapp2.0\publish\20180517003542-Adams.csv"
```

In this example, there were four files generated: `20180517003542-Adams.etl`, `20180517003542-Adams.xml`, `20180517003542-Adams.md`, `20180517003542-Adams.csv`

### Example 3: Running all benchmarks ###

```cmd
REM WARNING! The command below will run all the CoreClr benchmarks.
cd PerformanceHarness/bin/x64/Release/netcoreapp2.0/publish
dotnet PerformanceHarness.dll --perf:collect stopwatch+gcapi
```
