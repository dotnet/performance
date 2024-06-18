# Basic Scenarios

An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](./scenarios-workflow.md). The current document has specific instruction to run:

- [Basic Scenarios](#basic-scenarios)
  - [Basic Startup Scenarios](#basic-startup-scenarios)
  - [Basic Size On Disk Scenarios](#basic-size-on-disk-scenarios)
    - [Step 1 Initialize Environment](#step-1-initialize-environment)
    - [Step 2 Run Precommand](#step-2-run-precommand)
    - [Step 3 Run Test](#step-3-run-test)
    - [Step 4 Run Postcommand](#step-4-run-postcommand)
  - [Command Matrix](#command-matrix)
  - [Relevant Links](#relevant-links)

## Basic Startup Scenarios

Startup is a performance metric that measures the time to main (from process start to Main method) of a running application. [Startup Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/Startup) is a test harness that meausres throughputs in general, and the "TimeToMain" parser of it supports this metric and it's used in all of the **Basic Startup Scenarios**.

[Scenarios Tests Guide](./scenarios-workflow.md) already walks through **startup time of an empty console template** as an example. For other startup scenarios, refer to [Command Matrix](#command-matrix).

## Basic Size On Disk Scenarios

Size On Disk, as the name suggests, is a metric that recursively measures the sizes of a directory and its children. [4Disk Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/4Disk) is the test harness that provides this functionality and it's used in all of the **Basic Size On Disk Scenarios**.

We will walk through **Self-Contained Empty Console App Size On Disk** scenario as an example.

### Step 1 Initialize Environment

Same instruction of [Scenario Tests Guide - Step 1](./scenarios-workflow.md#step-1-initialize-environment).

### Step 2 Run Precommand

For **Self-Contained Empty Console App Size On Disk** scenario, run precommand to create an empty console template and publish it:

```cmd
cd emptyconsoletemplate
python3 pre.py publish -f net9.0 -c Release -r win-x64
```

`-f net9.0` sets the new template project targeting `net9.0` framework; `-c Release` configures the publish to be in release; `-r win-x64` takes an [RID](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)(Runtime Identifier) and specifies which runtime it supports.

**Note that by specifying RID option `-r <RID>`, it defaults to publish the app into a [SCD](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained)(Self-contained Deployment) app; without it, a [FDD](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-framework-dependent)(Framework Dependent Deployment) app will be published.**

Now there should be source code of the empty console template project under `app\` and published output under `pub\`.

### Step 3 Run Test

Now run the test:

```cmd
python3 test.py sod
```

[Size On Disk Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/4Disk) checks the default `pub\` directory and shows the sizes of the directory and its children:

```cmd
[2020/09/29 04:21:35][INFO] ----------------------------------------------
[2020/09/29 04:21:35][INFO] Initializing logger 2020-09-29 04:21:35.865708
[2020/09/29 04:21:35][INFO] ----------------------------------------------
[2020/09/29 04:21:35][INFO] $ C:\h\w\B0320981\p\SOD\SizeOnDisk.exe --report-json-path traces\perf-lab-report.json --scenario-name "SOD - New Console Template - SCD Publish" --dirs pub
[2020/09/29 04:21:36][INFO] SOD - New Console Template - SCD Publish
[2020/09/29 04:21:36][INFO] Metric                                                    |Average            |Min                |Max
[2020/09/29 04:21:36][INFO] ----------------------------------------------------------|-------------------|-------------------|-------------------
[2020/09/29 04:21:36][INFO] SOD - New Console Template - SCD Publish                  |69010285.000 bytes |69010285.000 bytes |69010285.000 bytes
[2020/09/29 04:21:36][INFO] SOD - New Console Template - SCD Publish - Count          |225.000 count      |225.000 count      |225.000 count
[2020/09/29 04:21:36][INFO] pub                                                       |69010285.000 bytes |69010285.000 bytes |69010285.000 bytes
[2020/09/29 04:21:36][INFO] pub - Count                                               |225.000 count      |225.000 count      |225.000 count
[2020/09/29 04:21:36][INFO] pub\api-ms-win-core-console-l1-1-0.dll                    |19208.000 bytes    |19208.000 bytes    |19208.000 bytes
[2020/09/29 04:21:36][INFO] pub\api-ms-win-core-datetime-l1-1-0.dll                   |18696.000 bytes    |18696.000 bytes    |18696.000 bytes
[2020/09/29 04:21:36][INFO] pub\api-ms-win-core-debug-l1-1-0.dll                      |18696.000 bytes    |18696.000 bytes    |18696.000 bytes
[2020/09/29 04:21:36][INFO] pub\api-ms-win-core-errorhandling-l1-1-0.dll              |18696.000 bytes    |18696.000 bytes    |18696.000 bytes
[2020/09/29 04:21:36][INFO] pub\api-ms-win-core-file-l1-1-0.dll                       |22280.000 bytes    |22280.000 bytes    |22280.000 bytes
[2020/09/29 04:21:36][INFO] pub\api-ms-win-core-file-l1-2-0.dll                       |18696.000 bytes    |18696.000 bytes    |18696.000 bytes
[2020/09/29 04:21:36][INFO] pub\api-ms-win-core-file-l2-1-0.dll                       |18696.000 bytes    |18696.000 bytes    |18696.000 bytes34
```

### Step 4 Run Postcommand

Same instruction of [Scenario Tests Guide - Step 4](./scenarios-workflow.md#step-4-run-postcommand).

## Command Matrix

- \<tfm> values:
  - netcoreapp3.1
  - net6.0
  - net7.0
  - net8.0
  - net9.0
- \<-r RID> values:
  - ""(WITHOUT `-r <RID>` --> FDD app)
  - `"-r <RID>"` (WITH `-r` --> SCD app, [list of RID](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog))

| Scenario                                      | Asset Directory         | Precommand                                    |  Testcommand    | Postcommand | Supported Framework                              | Supported Platform |
|-----------------------------------------------|-------------------------|-----------------------------------------------|-----------------|-------------|--------------------------------------------------|--------------------|
| Static Console Template Publish Startup       | staticconsoletemplate   | pre.py publish -f TFM -c Release           | test.py startup | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows            |
| Static Console Template Publish SizeOnDisk    | staticconsoletemplate   | pre.py publish -f TFM -c Release /<-r RID> | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| Static Console Template Build SizeOnDisk      | staticconsoletemplate   | pre.py build -f TFM -c Release             | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| Static VB Console Template Publish Startup    | staticvbconsoletemplate | pre.py publish -f TFM -c Release           | test.py startup | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows            |
| Static VB Console Template Publish SizeOnDisk | staticvbconsoletemplate | pre.py publish -f TFM -c Release /<-r RID> | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| Static VB Console Template Build SizeOnDisk   | staticvbconsoletemplate | pre.py build -f TFM -c Release             | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| Static Winforms Template Publish Startup      | staticwinformstemplate  | pre.py publish -f TFM -c Release           | test.py startup | post.py     | netcoreapp3.1        | Windows            |
| Static Winforms Template Publish SizeOnDisk   | staticwinformstemplate  | pre.py publish -f TFM -c Release /<-r RID> | test.py sod     | post.py     | netcoreapp3.1        | Windows;Linux      |
| Static Winforms Template Build SizeOnDisk     | staticwinformstemplate  | pre.py build -f TFM -c Release             | test.py sod     | post.py     | netcoreapp3.1        | Windows;Linux      |
| New Console Template Publish Startup          | emptyconsoletemplate    | pre.py publish -f TFM -c Release           | test.py startup | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows            |
| New Console Template Publish SizeOnDisk       | emptyconsoletemplate    | pre.py publish -f TFM -c Release /<-r RID> | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| New Console Template Build SizeOnDisk         | emptyconsoletemplate    | pre.py build -f TFM -c Release             | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| New VB Console Template Publish Startup       | emptyvbconsoletemplate  | pre.py publish -f TFM -c Release           | test.py startup | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows            |
| New VB Console Template Publish SizeOnDisk    | emptyvbconsoletemplate  | pre.py publish -f TFM -c Release /<-r RID> | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| New VB Console Template Build SizeOnDisk      | emptyvbconsoletemplate  | pre.py build -f TFM -c Release             | test.py sod     | post.py     | netcoreapp3.1;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |

## Relevant Links

- [SCD App](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained)
- [FDD App](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-framework-dependent)
