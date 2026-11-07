# Using Crank to schedule performance tests on Helix

## Table of Contents

- [Introduction](#introduction)
- [Current Limitations](#current-limitations)
- [Prerequisites](#prerequisites)
- [Workflow](#workflow)
  - [Building the runtime repository](#building-the-runtime-repository)
    - [Building for Windows on Windows](#building-for-windows-on-windows)
    - [Building for Linux on Windows](#building-for-linux-on-windows)
    - [Building for Windows arm64 on Windows](#building-for-windows-arm64-on-windows)
  - [Using the Crank CLI](#using-the-crank-cli)
    - [Example: Run microbenchmarks on Windows x64](#example-run-microbenchmarks-on-windows-x64)
    - [Profiles](#profiles)
    - [Other useful arguments](#other-useful-arguments)
  - [Accessing results](#accessing-results)
    - [Crank CLI output](#crank-cli-output)
    - [Azure Data Explorer](#azure-data-explorer)
    - [Crank JSON output](#crank-json-output)

## Introduction

We have support and documentation today explaining how to run the performance tests in this repository locally on your machine, however these steps may be quite difficult to follow, or you may not have the hardware that you wish to run the performance tests on. This document provides a way for internal Microsoft employees to schedule performance tests to be run on Helix machines using Crank.

- [Helix](https://github.com/dotnet/arcade/blob/main/Documentation/Helix.md) is the work scheduler that we use in our CI pipelines to run performance tests. We have many Helix queues available to us which provide different hardware capabilities so that we are able to test a wide array of situations.

- [Crank](https://github.com/dotnet/crank) is a tool that provides infrastructure for software performance measurement and is mainly used today to support our [TechEmpower Web Framework Benchmarks](https://github.com/aspnet/benchmarks).

## Current limitations

- This workflow is only available to Microsoft employees. If you are not a Microsoft employee you can continue to run benchmarks using our other [benchmarking workflow documentation](./benchmarking-workflow-dotnet-runtime.md).
- Currently, only support for running the BenchmarkDotNet benchmarks has been thoroughly tested. In the future, we will work towards adding support for scenarios such as Startup and Size on Disk.
- The developer is required to build the runtime repository themselves before sending the job to Helix. Doing the runtime builds on your local machine means you will be able to take full advantage of incremental compilation and won't have to wait for crank to build from scratch.
- There is no support currently for using an existing version of .NET installed using the .NET Installer. Only a local build of the .NET runtime is supported.

## Prerequisites

- The [dotnet/runtime](https://github.com/dotnet/runtime) and [dotnet/performance](https://github.com/dotnet/perforamnce) repositories must be cloned to your machine.
  - It is not required, but running crank will be simpler if the two repositories are cloned to the same parent directory such that doing `cd ../runtime` in the performance repository will navigate you to the runtime repository.
- Crank must be installed to your machine.
  - Only the crank controller is required. We are hosting a crank agent accessible to Microsoft employees which has all the required environment variables set up to schedule Helix jobs and upload performance results.
  - Crank can be installed with `dotnet tool install -g Microsoft.Crank.Controller --version "0.2.0-*"`
  - Please see the [crank](https://github.com/dotnet/crank) GitHub repository for further information and documentation.
- Microsoft's corporate VPN is required to be active to connect to the crank agent.
- Corpnet access
  - If you are working from home, it is likely that you are not on corpnet as corpnet usually requires that the machine is physically connected to a Microsoft Building.
  - If your machine is not connected to corpnet, then [DevBox](https://devbox.microsoft.com) is our strongly recommended alternative.
  - If you are unable to use DevBox and can't get corpnet access, then please email [dotnetperf@microsoft.com](mailto:dotnetperf@microsoft.com) so that we can give you an alternative.
- Additional configurations such as Mono, WASM, iOS, and Android are also not currently supported, but will be supported in the future.

## Workflow

### Building the runtime repository

The crank configuration only supports builds that have been generated to the Core_Root folder. Please see [these docs](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/coreclr/testing.md#building-the-core_root) in the runtime repo for more information about how to build this folder. If you wish to compile for a different OS or architecture, please read the [Cross-Building](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/cross-building.md#cross-building-for-different-architectures-and-operating-systems) documentation. Below are some examples of what to run for some common scenarios.

#### Building for Windows on Windows

Run the following in the cloned runtime repository

```cmd
.\build.cmd clr+libs -c Release
.\src\tests\build.cmd release generatelayoutonly
```

#### Building for Linux on Windows

Docker can be used to build for Linux ([see documentation](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/linux-instructions.md#build-using-docker)).
Ensure that you have Docker installed on your machine with WSL enabled. In the below script, RUNTIME_REPO_PATH should be a full path to the repo from the root.

```cmd
docker run --rm `
  -v <RUNTIME_REPO_PATH>:/runtime `
  -w /runtime `
  mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-22.04 `
  ./build.sh clr+libs -c Release && ./src/tests/build.sh release generatelayoutonly
```

#### Building for Windows arm64 on Windows

Run the following in the cloned runtime repository

```cmd
.\build.cmd clr+libs -c Release -arch arm64
.\src\tests\build.cmd arm64 release generatelayoutonly
```

### Using the Crank CLI

After installing crank as mentioned in the prerequisites, you will be able to invoke crank using `crank` in the command line.

#### Example: Run microbenchmarks on Windows x64

Below is an example of a crank command which will run any benchmarks with Linq in the name on a Windows x64 queue. This command must be run in the performance repository, and the runtime repository must be located next to it so that you could navigate to it with `cd ../runtime`.

```cmd
crank --config .\helix.yml --scenario micro --profile win-x64 --variable bdnArgs="--filter '*Linq*'" --profile msft-internal --variable buildNumber="myalias-20230811.1"
```

An explanation for each argument:

- `--config .\helix.yml`: This tells crank what yaml file defines all the scenarios and jobs
- `--scenario micro`: Runs the microbenchmarks scenario
- `--profile win-x64`: Configures crank to a local Windows x64 build of the runtime, and sets the Helix Queue to a Windows x64 queue.
- `--variable bdnArgs="--filter '*Linq*'"`: Sets arguments to pass to BenchmarkDotNet that will filter it to only Linq benchmarks
- `--profile msft-internal`: Sets the crank agent endpoint to the internal hosted crank agent
- `--variable buildNumber="myalias-20230811.1"`: Sets the build number which will be associated with the results when it gets uploaded to our storage accounts. You can use this to search for the run results in Azure Data Explorer. This build number does not have to follow any convention, the only recommendation would be to include something unique to yourself so that it doesn't conflict with other build numbers.

#### Profiles

Profiles are a set of predefined variables that are given a name so it is easy to reuse. Profiles are additive meaning that if you specify multiple profiles the variables will get merged together. A list of profiles can be found in [helix.yml](../helix.yml) at the bottom. Some of the profiles configure the crank agent endpoint, and other profiles configure what the target OS, architecture, and queue is. If you wish to run microbenchmarks on Ubuntu x64, just use `--profile ubuntu-x64`.

#### Other useful arguments

- `--variable runtimeRepoDir="../path/to/runtime"`: Set a custom path to the runtime repository, relative to the working directory
- `--variable performanceRepoDir="C:/path/to/performance"`: Set a custom path to the performance repository, relative to the working directory
- `--variable partitionCount=10`: Set the number of Helix jobs to split the microbenchmarks across. By default this is 5, but may need to be increased or decreased depending on the number of benchmarks being run. If running all the  microbenchmarks, it is recommended to set this to 30. If just running a few microbenchmarks, set this to 1.
- `--variable queue="Windows.11.Amd64.Tiger.Perf"`: Set a specific Helix queue to run on. When doing this you may need to set `osGroup`, `architecture`, and `internal` as well.
- `--variable osGroup="windows"`: Set what type of OS the helix queue is, examples of valid values are `windows`, `osx`, and `linux`, `ios`, `freebsd`, etc. Set to `windows` by default.
- `-variable architecture="x64"`: Sets the architecture to use e.g. `x64`, `x86`, `arm64`. Set to `x64` by default.
- `--variable internal="true"`: Sets whether or not the Helix Queue is a public or internal queue. If the queue is public the results will not be uploaded. Defaults to `true`.
- `--json results.json`: Will export all the raw benchmark results as a JSON with the given file name.

### Accessing results

#### Crank CLI output

Once the helix jobs have completed, crank will output a simplfied benchmark results with a list of all the benchmarks and the average runtime.

#### Azure Data Explorer

If you made use of a non-public queue, the results will be uploaded our [Azure Data Explorer](https://dataexplorer.azure.com/clusters/dotnetperf.westus/databases/PerformanceData) database and be accessible almost immediately to query. If you don't have access to see the Azure Data Explorer database, please join the ".NET Perf Data Readers" Security Group.

Using the `buildNumber` you set on the command line, you can search for that build number in the "Build Name" column in the Measurements table. Using the build number from earlier `myalias-20230811.1` as an example, you could query for your data with the following:

```kql
Measurements
| where BuildName == "myalias-20230811.1"
| where TestCounterDefaultCounter // filter to only the default counter
```

This will contain much more information about each benchmark including standard deviation and all the individual measurements

#### Crank JSON output

If you want access to the raw data but don't wish to use Azure Data Explorer, you can also pass the `--json results.json` command line argument to crank and you will also get raw measurements data which you can look at.
