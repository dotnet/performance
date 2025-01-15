# .NET Performance Repository Documentation Readme

The documentation in this repo is organized into the following sections:

## Getting Started

- [Prerequisites](prerequisites.md) - Information on what you need to get started.
- [Perf report walkthrough](perfreport-walkthrough.md) - This document describes Performance Report Advanced Features.
- [Crank to Helix workflow](crank-to-helix-workflow.md) - Information on how to schedule performance tests to be run on Helix machines using Crank (instead of running locally).
- [Profiling workflow dotnet runtime](profiling-workflow-dotnet-runtime.md) - This doc explains how to profile local [dotnet/runtime](https://github.com/dotnet/runtime) builds and it's targetted at [dotnet/runtime](https://github.com/dotnet/runtime) repository contributors.
- [Pipeline templates](../eng/common/template-guidance.md) - Information on azure yml pipelines.

## Running Benchmarks

- [BenchmarkDotNet](benchmarkdotnet.md) - Information of how to run benchmarks using BenchmarkDotNet tool and interpret results.
- [Benchmarking workflow](benchmarking-workflow-dotnet-runtime.md) - Information about the (micro)benchmarks for the [dotnet/runtime](https://github.com/dotnet/runtime) in this repository.
- [Microbenchmarks Guide](../src/benchmarks/micro/README.md) for information on running our microbenchmarks.
- [Micro Benchmarks of .NET Runtime(s)](../src/benchmarks/micro/README.md) - Information on benchmarks of .NET Runtime(s).
- [Microbenchmarks design guidelines](microbenchmark-design-guidelines.md) - Detailed guidelines on how to design and write microbenchmarks.
- [benchmarks_local.py script guide](../scripts/BENCHMARKS_LOCAL_README.md) - Description of a script for testing the performance of the different dotnet/runtime build types locally.
- [ResultsComparer tool](../src/tools/ResultsComparer/README.md) - Information on tool which allows for easy comparison of provided benchmark results.
- [Serializers Benchmarks](../src/benchmarks/micro/Serializers/README.md) - Information on benchmarks of the most popular serializers.
- [bepuphysics2 Benchmarks](../src/benchmarks/real-world/bepuphysics2/README.md) - Information on benchmarks of bepuphysics2 library.
- [Microsoft.ML Benchmarks datasets](../src/benchmarks/real-world/Microsoft.ML.Benchmarks/Input/README.md) - Information on datasets used for benchmarking of the Microsoft.ML library.
- [Benchmarks run in PowerShell](../src/benchmarks/real-world/PowerShell.Benchmarks/README.md) - Information on performance tests for different pieces of the library run using PowerShell.

### GC Benchmarks

- [ASP.NET Benchmarks errors](../src/benchmarks/gc/GC.Infrastructure/docs/ASPNETBenchmarks.md) - Information on main types of errors while running ASP.NET Benchmarks using crank.
- [Testing GC.Infrastructure](../src/benchmarks/gc/GC.Infrastructure/README.md) - Information on testing GC.Infrastructure.
- [GC.Analysis.API](../src/benchmarks/gc/GC.Infrastructure/GC.Analysis.API/README.md) - Information on conducting GC, CPU and Threading analysis using .NET Interactive notebooks.
- [GC.Infrastructure Notebooks](../src/benchmarks/gc/GC.Infrastructure/Notebooks/README.md) - Information on notebooks that either provide examples or functionality for specialized analysis
- [Benchmark Analysis](../src/benchmarks/gc/GC.Infrastructure/Notebooks/BenchmarkAnalysis.md) - Information on a notebook which contains code for producing charts (and soon, tables) for GC benchmarks. It can currently process data
from the ASP.NET benchmarks obtained using crank as well as ETL data.

## Running Scenarios

- [Scenarios workflow](scenarios-workflow.md) - An introduction of how to run scenario tests.
- [Basic scenarios](basic-scenarios.md) - Specific instruction of how to run various basic scenarios.
- [Blazor scenarios](blazor-scenarios) - Specific instruction of how to run _New Blazorwasm Template Size On Disk_ scenarios.
- [Cross-gen scenarios](crossgen-scenarios.md) - Specific instruction of how to run _crossgen_ scenarios.
- [SDK scenarios](sdk-scenarios.md) - Specific instruction of how to run _SDK Build Throughput Scenario_ scenarios.
