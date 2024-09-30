# GC.Infrastructure

This repository contains the code to invoke the GC Infrastructure that currently runs the following types of test suites for a specific baseline and a run for which you want to test the performance for:

1. GCPerfSim
2. Microbenchmarks
3. ASP.NET Benchmarks
4. Test.

Currently, the infrastructure runs exclusively on Windows if you want to run the scenarios locally.

## Workflow

This section details the end-to-end workflow associated with getting the infrastructure to run for the GCPerfSim, Microbenchmark and ASP.NET Benchmark test suites.

### 1. Prerequisites

1. Clone the performance repo: ``git clone https://github.com/dotnet/performance C:\performance\``.
2. Install:
   1. The Dotnet 7 SDK.
      1. The link to the installers can be found [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).
   2. [crank](https://github.com/dotnet/crank)
      1. crank can be installed by invoking: ``dotnet tool install Microsoft.Crank.Controller --version "0.2.0-*" --global``.
3. Ensure that your machine can connected to Corp Net for the ASP.NET Scenarios. You can still run GCPerfSim and Microbenchmark test suites without being connected to Corp Net.
4. Invoke the rest of the commands in Admin Mode.

### 2. Building the Infrastructure

The next step is to build the Infrastructure. To build the infrastructure in Release Mode do the following steps:

1. ``cd C:\performance\src\benchmarks\gc\GC.Infrastructure\GC.Infrastructure``.
2. ``dotnet build -c Release``.

### 3. Running the Infrastructure

To run all the test suites, do the following steps:

1. ``cd C:\performance\src\benchmarks\gc\GC.Infrastructure\GC.Infrastructure``.
2. Ensure you have the right fields set in ``C:\performance\src\benchmarks\gc\GC.Infrastructure\Configurations\Run.yaml`` for:
    1. The output path. As an example, by default, the results will be stored in ``C:\InfraRuns\Run``.
        1. Ensure the output directory is empty as the analysis code will pick up the older traces and will interfere with the results generation.
    2. The gcperfsim path: The path of the GCPerfSim.dll.
        1. This dll can be built by the following steps:
            1. ``cd C:\performance\src\benchmarks\gc\src\exec\GCPerfSim``.
            2. ``dotnet build -c Release``.
            3. The path of GCPerfSim.dll will be available in: ``C:\performance\artifacts\bin\GCPerfSim\Release\{.NET Version}\GCPerfSim.dll``.
                1. For example: C:\Performance\artifacts\bin\GCPerfSim\Release\net7.0\GCPerfSim.dll.
    3. The path to the microbenchmark folder or the root path of the Microbenchmarks projects which, will be in: ``C:\performance\src\benchmarks\micro``.
        1. Ensure that the microbenchmarks have been compiled using: ``dotnet build -c Release``.
    4. The corerun path for the baseline and the run.
3. Invoke: ``dotnet run -- run --configuration C:\performance\src\benchmarks\gc\GC.Infrastructure\Configuration\Run.yaml``.

The aggregate results for each of the test suites will be written in ``C:\InfraRuns\Run\Result.md``.

#### Running the Infrastructure on A Machine With Just The Binaries

As an aside, you can run the infrastructure with just the binaries on a machine without the source. To achieve this do the following steps:

1. Copy the contents from ``C:\performance\artifacts\bin\GC.Infrastructure\Release\net7.0\`` onto the machine you wish to run the infrastructure on such as ``C:\InfrastructureBinary\``.
2. Copy the contents of the Configurations from ``C:\performance\src\benchmarks\gc\GC.Infrastructure\Configuration\`` onto the machine you wish the infrastructure on such as ``C:\InfrastructureConfigurations``.
3. ``cd C:\InfrastructureBinary``.
4. ``.\GC.Infrastructure.exe run --configuration C:\InfrastructureConfigurations\Run.yaml``.

#### Running Individual Test Suites

##### GCPerfSim

To run a specific GCPerfSim scenario such as the Normal Server scenario, invoke the following command:

1. ``cd C:\performance\artifacts\bin\GC.Infrastructure\Release\net7.0\``.
2. ``.\GC.Infrastructure.exe gcperfsim --configuration C:\InfrastructureConfigurations\GCPerfSim\Normal_Server.yaml``.

###### Running GCPerfSim Scenarios on the ASP.NET Machines

To run GCPerfSim on the ASP.NET Machines, do the following:

1. Ensure you are connected to Corp Net.
2. Pass in the name of the machine name as the optional ``--server`` parameter.
   1. For example: ``.\GC.Infrastructure.exe gcperfsim --configuration C:\InfrastructureConfigurations\GCPerfSim\Normal_Server.yaml --server aspnet-perf-win``.
3. To run GCPerfSim scenarios via crank locally, set the ``--server`` to ``local`` after ensuring that you are running the crank-agent.
   1. To install ``crank-agent``, invoke:
      1. ``dotnet tool install -g Microsoft.Crank.Agent --version "0.2.0-*"``.
      2. ``sc.exe create "CrankAgentService" binpath= "%USERPROFILE%\crank-agent.exe --url http://*:5001 --service"``.
   2. Then run the crank-agent by invoking ``crank-agent`` locally.

The list of machines you can choose to run the configuration can be found [here](https://github.com/aspnet/Benchmarks/tree/main/scenarios#profiles).

##### Microbenchmarks

To run the infrastructure on a specific Microbenchmark scenario such as the Server case invoke the following command:

1. ``cd C:\performance\artifacts\bin\GC.Infrastructure\Release\net7.0\``.
2. ``.\GC.Infrastructure.exe microbenchmarks --configuration C:\InfrastructureConfigurations\Microbenchmark\Microbenchmark_Server.yaml``.

###### How To Update The Microbenchmarks To Run

If you wish to change the microbenchmarks you'd wish to run, you can edit the txt file that's referenced in the Microbenchmark Configuration file or reference another file in the ``filter_path`` of the ``microbenchmark_configurations``:

```yaml
microbenchmark_configurations:
  filter: 
  filter_path: C:\InfraRuns\RunNew_All\Suites\Microbenchmark\MicrobenchmarksToRun.txt # CHANGE THIS.
  dotnet_installer: net7.0
  bdn_arguments: --warmupCount 1 --iterationCount 20 --allStats --outliers DontRemove --keepFiles
```

As an example, if you want to _just_ run the "System.IO.Tests.Perf_File.ReadAllBytes(size: 104857600)" microbenchmark, the steps to run the infrastructure would be:

1. Creating a new text file: ``notepad C:\performance\src\benchmarks\gc\GC.Infrastructure\Configurations\Microbenchmark\MicrobenchmarksToRun.txt``.
2. Updating the text file with: ``"System.IO.Tests.Perf_File.ReadAllBytes(size: 104857600)"``.
   1. Note: If you want add more microbenchmarks, they should be separated by the ``|`` operator. As an example, if you want to also add all V8 tests, the file would be: ``"System.IO.Tests.Perf_File.ReadAllBytes(size: 104857600)" | "V8.*"``
3. Updating the filter_path in ``C:\performance\src\benchmarks\gc\GC.Infrastructure\Configurations\Microbenchmark\Microbenchmarks_Server.yaml`` to point to MicrobenchmarksToRun.txt:
   1. ``filter_path: C:\performance\src\benchmarks\gc\GC.Infrastructure\Configurations\Microbenchmark\MicrobenchmarksToRun.txt``.
4. Running the infrastructure:
   1. ``cd C:\performance\artifacts\bin\GC.Infrastructure\Release\net7.0\``.
   2. ``.\GC.Infrastructure.exe microbenchmarks --configuration C:\InfrastructureConfigurations\Microbenchmark\Microbenchmark_Server.yaml``.

###### Using Cached Invocation Counts

The microbenchmarks run by the GC instructure must have the same invocation counts amongst the various comparative runs; this is important as we want to conduct an extremely fair comparison between the comparands. To ensure the same invocation counts are used amongst the runs, the value must be discerned before hand or in other words, we must do a dry run of the benchmark and parse the invocation count out of the results and then use that value while running it for the different microbenchmarks.

To save time, you can pass in a "psv" file (the psv stands for pipe separated values) of the following format that contains the name of the benchmark and the invocation count that can be used. Without providing this file, the infrastructure will run the test run to discern the invocation count:

```psv
Benchmark|InvocationCount
System.Numerics.Tests.Perf_BigInteger.Add(arguments: 65536*|135392
System.Tests.Perf_GC<Byte>.AllocateArray(length: 1000, *|4382544
System.Tests.Perf_GC<Char>.AllocateArray(length: 1000, *|2700576
```

The path to this file can be passed in as an optional argument for the ``microbenchmarks`` command and can be invoked in the following manner:

1. ```cd C:\Infrastructure\GC.Infrastructure\bin\Release\net7.0```.
2. ``.\GC.Infrastructure.exe microbenchmarks --configuration  C:\Infrastructure\Configurations\Microbenchmark\Microbenchmarks_Server.yaml --invocationCountPSV C:\GC.Analysis.API\Configurations\Microbenchmark\MicrobenchmarkInvocationCounts.psv``.

##### ASP.NET Benchmarks

To run the infrastructure on a specific set of ASP.NET Benchmarks, do the following:

1. ``cd C:\performance\artifacts\bin\GC.Infrastructure\Release\net7.0\``.
2. ``.\GC.Infrastructure.exe aspnetbenchmarks --configuration C:\performance\src\benchmarks\gc\GC.Infrastructure\Configurations\ASPNetBenchmarks\ASPNetBenchmarks.yaml``.

More details about running and troubleshooting ASP.NET benchmarks can be found [here](./docs/ASPNETBenchmarks.md).

###### Uploading Your Own Binaries

The ASP.NET benchmarks can be run without any of the users changes however, if the user wants to upload modified binaries with their changes, it is advisable to only upload those as long as they are compatible with the version of .NET runtime you wish to test against. The infrastructure allows you to either upload a single binary or a directory with one or more binaries.

This can be accomplished by specifying either a file or a directory as the corerun path of a particular run:

As an example, if I were to only update ``gc.cpp`` and build a standalone ``clrgc.dll``, specifically set the ``corerun`` field of the said run to the path of the ``clrgc.dll``.
NOTE: the environment variable ``DOTNET_GCName`` must be set in this case:

1. Assume your ``clrgc.dll`` is placed in ``C:\ASPNETUpload``:  

```powershell
C:\ASPNETUPLOAD
|-----> clrgc.dll
```

2. Adjust the corerun to point to the path of clrgc.dll:

```yaml
runs:
  run:
    corerun: C:\ASPNetUpload\clrgc.dll
    environment_variables:
      DOTNET_GCName: clrgc.dll # This environment variable was set.
```

NOTE: For this case, ensure the environment variable ``DOTNET_GCName`` is set to clrgc.dll.

On the other hand, if you want upload the entire directory, say ``C:\ASPNETUpload2``, simply set the path to the directory in the corerun of a corerun:

```yaml
runs:
  run:
    corerun: C:\ASPNetUpload2
    environment_variables:
      DOTNET_GCName: clrgc.dll
```

###### Updating Which Benchmarks to Run

The file that dictates which ASP.NET benchmarks to run is a CSV file and can be configured based on what test you need to run; an example of this file can be found [here](./Configurations/ASPNetBenchmarks/ASPNetBenchmarks.csv).

You can update this file by changing the following field:

```yaml
benchmark_settings:
  benchmark_file: C:\InfraRuns\RunNew_All\Suites\ASPNETBenchmarks\ASPNetBenchmarks.csv
```

The format of this file is:

``Legend,Base CommandLine``

where:

1. Legend column should contain the name of the ASP.NET benchmark followed by an underscore and the name of the OS.
2. The Base CommandLine is the base crank command that's run. More details about how to appropriately set this can be found [here](#how-to-add-new-benchmarks).

It's worth noting that if you have specified Linux based binaries in the corerun path, the Windows based ASP.NET benchmarks will exhibit undefined behavior.

###### How To Add New Benchmarks

1. If you are collecting traces, make sure to include Linux (_Linux) or Windows (_Windows) suffix in the Legend column because we run PerfView to collect traces for Windows and dotnet-trace for `gc` trace; currently not working for other types of traces on Linux.
2. Find the base command line for the benchmark to run by choosing the appropriate test and configuration from the [ASP.NET Dashboard](https://msit.powerbi.com/groups/me/reports/10265790-7e2e-41d3-9388-86ab72be3fe9/ReportSection30725cd056a647733762?experience=power-bi)
3. Copy over the command line from the table to the Base CommandLine column after:
   1. Remove the ``crank`` prefix from the command line.
   2. Remove the ``--application.aspNetCoreVersion``, ``--application.runtimeVersion`` and ``--application.sdkVersion`` command args from the command line that you paste in the CSV as the versions are set by the infrastructure itself.

###### How To Filter Benchmarks

You can filter benchmarks of interest from the entire set of benchmarks specified by the referenced `benchmarks_file` using a list of regex patterns such as the following in the `benchmark_settings` section:

```yaml
benchmark_settings:
  benchmark_filters:
  - Stage1Aot_Windows*
  - PlainText*
```

If there is a match, these filters will run in the order specified in the yaml file.

###### How To Override Parameters

You can override parameters specified in the benchmark csv file by replacing all instances of the command arg with values in the `override_arguments` field.

```yaml
benchmark_settings:
  benchmark_file: C:\InfraRuns\RunNew_All\Suites\ASPNETBenchmarks\ASPNetBenchmarks.csv
  additional_arguments: --chart --chart-type hex 
  override_arguments: --profile aspnet-citrine-win
```

As an example based on the configuration immediately above, all `--profile` values will be replaces with `--profile aspnet-citrine-win`.

## All Commands

The infrastructure can be run in modular manner. What this means is that you can invoke a particular command that runs some part of the infrastructure. A list of all the commands can be found here:

| Command Name             | Description                                                                                   | Example                                                     |
|--------------------------|-----------------------------------------------------------------------------------------------|-------------------------------------------------------------|
| run                      | Creates the suite, runs the tests and generates the top level report.                         | ``run --configuration InputConfiguration.yaml``                 |
| createsuites             | Creates the suites.                                                                           | ``createsuites --configuration InputConfiguration.yaml``        |
| run-suite                | Runs the suite.                                                                               | ``run-suite --suiteBasePath Path``                              |
| gcperfsim                | Runs a GCPerfSim Configuration - both orchestration and analysis.                             | ``gcperfsim --configuration Configuration.yaml [--server nameOfMachine]``                |
| gcperfsim-analyze        | Runs just the analysis portion of the GCPerfSim run assuming the traces are available.        | ``gcperfsim-analyze --configuration Configuration.yaml``        |
| gcperfsim-compare        | Runs the comparison between two traces and generates a report for GCPerfSim runs. The acceptable file types are: ``.etl, .nettrace, .etl.zip``            | ``gcperfsim-compare --baseline Trace1Path  --comparand Trace2Path --output PathToOutput.md``        |
| gcperfsim-functional | Runs the functional portion of the GCPerfSim Tests. | ``gcperfsim-functional --configuration Configuration.yaml`` |
| microbenchmarks          | Runs a Microbenchmark Configuration - both orchestration and analysis.                        | ``microbenchmarks --configuration Configuration.yaml``           |
| microbenchmarks-analyze  | Runs just the analysis portion of the Microbenchmark run assuming the traces are available.   | ``microbenchmarks-analyze --configuration Configuration.yaml``   |
| aspnetbenchmarks         | Runs the ASPNet Benchmarks - both orchestration and analysis.                                 | ``aspnetbenchmarks --configuration Configuration.yaml``         |
| aspnetbenchmarks-analyze | Runs just the analysis portion of the ASPNet benchmark run assuming the traces are available. | ``aspnetbenchmarks-analyze --configuration Configuration.yaml`` |
