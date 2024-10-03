# .NET Performance Repository Documentation Readme

The documentation in this repo is organized into the following sections:

## Getting Started

- [Prerequisites](prerequisites.md) - Information on what you need to get started.
- [Perfreport walktrough](perfreport-walktrough.md) - This document describes Performance Report Advanced Features.
- [Crank to Helix workflow](crank-to-helix-workflow.md) - Information on how to schedule performance tests to be run on Helix machines using Crank (instead of running locally).
- [Profiling workflow dotnet runtime](profiling-workflow-dotnet-runtime.md) - This doc explains how to profile local [dotnet/runtime](https://github.com/dotnet/runtime) builds and it's targetted at [dotnet/runtime](https://github.com/dotnet/runtime) repository contributors.

## Running Benchmarks

- [BenchmarkDotNet](benchmarkdotnet.md) - Information of how to run benchmarks using BenchmarkDotNet tool and interpret results.
- [Benchmarking workflow](benchmarking-workflow-dotnet-runtime.md) - Information about the (micro)benchmarks for the [dotnet/runtime](https://github.com/dotnet/runtime) in this repository.
- [Microbenchmarks Guide](../src/benchmarks/micro/README.md) for information on running our microbenchmarks.
- [Microbenchmarks design guidelines](microbenchmark-design-guidelines.md) - Detailed guidelines on how to design and write microbenchmarks.

## Running Scenarios

- [Scenarios workflow](scenarios-workflow.md) - An introduction of how to run scenario tests.
- [Basic scenarios](basic-scenarios.md) - Specific instruction of how to run various basic scenarios.
- [Blazor scenarios](blazor-scenarios) - Specific instruction of how to run _New Blazorwasm Template Size On Disk_ scenarios.
- [Cross-gen scenarios](crossgen-scenarios.md) - Specific instruction of how to run _crossgen_ scenarios.
- [SDK scenarios](sdk-scenarios.md) - Specific instruction of how to run _SDK Build Throughput Scenario_ scenarios.
