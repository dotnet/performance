
# Blazor Scenarios

An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](./scenarios-workflow.md). The current document has specific instruction to run:

- [New Blazorwasm Template Size On Disk](#new-blazorwasm-template-size-on-disk)

## New Blazorwasm Template Size On Disk

**New Blazorwasm Template Size On Disk** is a scenario test that meausres the size of published output of blazorwasm template. In other words, our test harness *implicitly* calls

```cmd
dotnet new blazorwasm
```

and then

```cmd
dotnet publish -c Release -o pub
```

and measures the sizes of the `pub\` directory and its children with [SizeOnDisk Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/SizeOnDisk).

**For more information about scenario tests in general, an introduction of how to run scenario tests can be found in [Scenario Tests Guide](link).  The current document has specific instruction to run blazor scenario tests.**

### Prerequisites

- python3 or newer
- dotnet runtime 6.0 or newer

### Step 1 Initialize Environment

Same instruction of [Step 1 in Scenario Tests Guide](scenarios-workflow.md#step-1-initialize-environment).

### Step 2 Run Precommand

Run precommand to create and publish a new blazorwasm template:

```cmd
cd blazor
python3 pre.py publish --msbuild "/p:_TrimmerDumpDependencies=true"
```

Now there should be source code of the blazorwasm project under `app\` and published output under `pub\`. The `--msbuild "/p:_TrimmerDumpDependencies=true"` argument is optional and can be added to generate [linker dump](https://github.com/mono/linker/blob/main/src/analyzer/README.md) from the build, which will be saved to `blazor\app\obj\<Configuration>\<Runtime>\linked\linker-dependencies.xml.gz`.

### Step 3 Run Test

Run testcommand to measure the size on disk of the published output:

```cmd
py -3 test.py sod --scenario-name "SOD - New Blazor Template - Publish"
```

In the command, `sod` refers to the "Size On Disk" metric and [SizeOnDisk Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/SizeOnDisk) will be used for this scenario. Note that `--scenario-name` is optional and the value can be changed for your own reference.

The test output should look like the following:

```cmd
[2020/09/25 11:24:44][INFO] ----------------------------------------------
[2020/09/25 11:24:44][INFO] Initializing logger 2020-09-25 11:24:44.727500
[2020/09/25 11:24:44][INFO] ----------------------------------------------
[2020/09/25 11:24:44][INFO] $ C:\repos\performance-docs\artifacts\SOD\SizeOnDisk.exe --report-json-path traces\perf-lab-report.json --scenario-name "SOD - New Blazor Template - Publish" --dirs pub
[2020/09/25 11:24:46][INFO] SOD - New Blazor Template - Publish
[2020/09/25 11:24:46][INFO] Metric
                              |Average            |Min                |Max
[2020/09/25 11:24:46][INFO] -----------------------------------------------------------------------------------------|-------------------|-------------------|-------------------
[2020/09/25 11:24:46][INFO] SOD - New Blazor Template - Publish
                              |27527723.000 bytes |27527723.000 bytes |27527723.000 bytes
[2020/09/25 11:24:46][INFO] Total Uncompressed _framework
                              |15820983.000 bytes |15820983.000 bytes |15820983.000 bytes
[2020/09/25 11:24:46][INFO] Total Uncompressed _framework - Count
                              |72.000 count       |72.000 count       |72.000 count
[2020/09/25 11:24:46][INFO] Synthetic Wire Size - .br
```

[SizeOnDisk Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/SizeOnDisk) recursively measures the size of each folder and its children under the specified directory. In addition to the folders and files (path-like counters such as `pub\wwwroot\_framework\blazor.webassembly.js.gz` ), it also generates aggregate counters for each file type (such as `Aggregate - .dll`). For this **New Blazorwasm Template Size On Disk** scenario, Counter names starting with `Synthetic Wire Size` is a unique counter type for blazorwasm, which simulates the size of files actually transferred over the wire when the webpage loads.

### Step 4 Run Postcommand

Same instruction of [Step 4 in Scenario Tests Guide](scenarios-workflow.md#step-4-run-postcommand).

## Command Matrix

For the purpose of quick reference, the commands can be summarized into the following matrix:

| Scenario                            | Asset Directory | Precommand                                                  | Testcommand                                                       | Postcommand | Supported Framework | Supported Platform |
|-------------------------------------|-----------------|-------------------------------------------------------------|-------------------------------------------------------------------|-------------|---------------------|--------------------|
| SOD - New Blazor Template - Publish | blazor          | pre.py publish --msbuild "/p:_TrimmerDumpDependencies=true" | test.py sod --scenario-name "SOD - New Blazor Template - Publish" | post.py     | net7.0; net8.0      | Windows;Linux      |

## Relevant Links

- [Blazorwasm](https://github.com/dotnet/aspnetcore/tree/main/src/Components)
- [IL Linker](https://github.com/mono/linker)
