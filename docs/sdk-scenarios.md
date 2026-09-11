# SDK Scenarios

An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](./scenarios-workflow.md). The current document has specific instruction to run:

- [SDK Build Throughput Scenario](#sdk-build-throughput-scenario)

## SDK performance pipeline

The repository's [azure-pipelines.yml](../azure-pipelines.yml) entrypoint uses
[sdk-perf-jobs.yml](../eng/pipelines/sdk-perf-jobs.yml) for SDK-based scenario and
benchmark jobs. It backs the internal `dotnet-performance` pipeline (definition 306)
and the public `performance-ci` pipeline (definition 38).

The entrypoint always enables asynchronous submission and includes the standalone
Helix Job Monitor, its pool provider, and its parameters, including on public and PR
runs. The monitor runs alongside submitters in the same implicit stage, waits for
their Helix work items, and reports failures to Azure DevOps. The existing manual
job-selection parameters and schedules are unchanged: public runs always select
correctness jobs, while internal manual runs can select no workloads. Such runs still
include the monitor and may succeed without Helix jobs (`allowNoHelixJobs: true`).
This permits an empty stage; it does not suppress submitter or work-item failures.

The monitor job has a six-hour timeout (355 minutes for the tool, leaving five minutes
for it to exit). Existing 320-minute submitter limits and Helix work-item timeouts are
unchanged. These limits start when the respective job runs; they are not a whole-run
wall-clock deadline that includes time waiting for agents.

Only internal non-PR runs import `DotNet-HelixApi-Access`. The always-present token
parameter uses a compile-time expression to select its token macro for those runs
and an empty string otherwise. Public and PR monitors therefore use anonymous Helix
access, not an unresolved private-token variable. Azure DevOps timeline access and
result reporting still use the job's `System.AccessToken`. The tool is restored from
`.config/dotnet-tools.json` in its own checkout; `eng/Version.Details.xml` tracks that
pin alongside the Helix SDK.

Monitor test-run names include the matrix-expanded job display name. This keeps
different channels in the same phase and Helix queue independent while preserving a
stable identity when the same leg is retried.

Submitter build logs still use the existing `Logs_*` pipeline artifacts. Benchmark
results and diagnostics are still uploaded from the Helix work items to PerfLab and
Helix, respectively. The monitor reports test results and links to Helix consoles; it
does not download arbitrary Helix uploads into pipeline artifacts. Asynchronous sends
skip the submitter's `artifacts/helix-results` downloads, which were not published by
this pipeline. Local `--send-to-helix` runs still wait and download performance reports.

## SDK Build Throughput Scenario

**SDK Build Throughput** is a scenario test that measures the throughput of SDK build process. To be more specific, our test *implicitly calls*

```cmd
dotnet build <project>
```

with other applicable arguments and measures its throughput.

There are 2 types of SDK build --- *Clean Build* and *Build No Change*.

- *Clean Build*: simulates the first-time-ever build of the project, when binaries do not exist in the output folder. Between each iteration, the test harness cleans the output folder and turns off dotnet servers.
- *Build No Change* simulates the build after the first-time-ever build. The test harness runs a warmup build which leaves the binaries, without cleanup between iterations.

### Prerequisites

- make sure the test directory is clean of artifacts (you can run `post.py` to remove existing artifacts folders from the last run )
- python3 or newer
- dotnet runtime 3.1 or newer
- terminal/command prompt **in Admin Mode** (for collecting kernel traces)
- clean state of the test machine (anti-virus scan is off and no other user program's running -- to minimize the influence of environment on the test)

We will walk through **SDK Console Template** as an example.

### Step 1 Initialize Environment

Same instruction of [Scenario Tests Guide - Step 1](./scenarios-workflow.md#step-1-initialize-environment).

### Step 2 Run Precommand

If you are running the test NOT for the first time and `app\` folder exists under the asset directory, make sure it's removed so the previous test artifact won't be used. You can use `post.py` to clean it up:

```cmd
python3 post.py
```

Run precommand to create a new console template.

```cmd
cd emptyconsoletemplate
python3 pre.py default -f net9.0
```

The `default` command prepares the asset (creating a new project if the asset is a template and copying it to `app\`). Now there should be source code of a console template project under `app\`.

Note that it is important to be aware SDK takes different paths for different TFMs, and you can configure which TFM your SDK tests against. Howeverm your SDK version should be >= the TFM version because SDK cannot build a project that has a newer runtime. Here's a matrix of valid SDK vs. TFM combinations:

|              | netcoreapp2.1 | netcoreapp3.1 | net5.0 | net6.0 | net7.0 | net8.0 | net9.0 |
|--------------|---------------|---------------|--------|--------|--------|--------|--------|
| .NET 2.1 SDK | x             |               |        |        |        |        |        |
| .NET 3.1 SDK | x             | x             |        |        |        |        |        |
| .NET 5 SDK   | x             | x             | x      |        |        |        |        |
| .NET 6 SDK   | x             | x             | x      | x      |        |        |        |
| .NET 7 SDK   | x             | x             | x      | x      | x      |        |        |
| .NET 8 SDK   | x             | x             | x      | x      | x      | x      |        |
| .NET 9 SDK   | x             | x             | x      | x      | x      | x      | x      |

You can change TFM of the project by specifying `-f <tfm>`, which allows to replace the `<TargetFramework></TargetFramework>` property in the project file to be the custom TFM value (make sure it's a valid TFM value) you specified.

### Step 3 Run Testcommand

Run testcommand to measure the throughput of sdk build.

For *Clean Build* test, run:

```cmd
python3 test.py sdk clean_build
```

For *Build No Change* test, run:

```cmd
python3 test.py sdk build_no_change
```

The test result should look like the following:

```cmd
[2020/09/27 23:51:22][INFO] Merging traces\emptycsconsoletemplate_SDK_build_no_change_startup.perflabkernel.etl...
[2020/09/27 23:51:22][INFO] Trace Saved to traces\emptycsconsoletemplate_SDK_build_no_change_startup.etl
[2020/09/27 23:51:22][INFO] Parsing traces\emptycsconsoletemplate_SDK_build_no_change_startup.etl
[2020/09/27 23:51:23][INFO]
[2020/09/27 23:51:23][INFO] Metric         |Average        |Min            |Max
[2020/09/27 23:51:23][INFO] ---------------|---------------|---------------|---------------
[2020/09/27 23:51:23][INFO] Process Time   |1621.667 ms    |1587.230 ms    |1706.269 ms
[2020/09/27 23:51:23][INFO] Time on Thread |969.075 ms     |414.988 ms     |1347.836 ms
```

### Step 4 Run Postcommand

Same instruction of [Step 4 in Scenario Tests Guide](scenarios-workflow.md#step-4-run-postcommand).

## Command Matrix

- \<tfm> values:
  - netcoreapp2.1
  - netcoreapp3.1
  - net5.0
  - net6.0
  - net7.0
  - net8.0
  - net9.0
- \<build option> values:
  - clean_build
  - build_no_change

| Scenario                      | Asset Directory      | Precommand               | Testcommand                 | Postcommand | Supported Framework                       | Supported Platform |
|:------------------------------|:---------------------|:-------------------------|:----------------------------|:------------|:------------------------------------------|:-------------------|
| SDK Console Template          | emptyconsoletemplate | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp2.1;netcoreapp3.1;net5.0;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| SDK .NET 2.0 Library Template | netstandard2.0       | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp2.1;netcoreapp3.1;net5.0;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
| SDK ASP.NET MVC App Template  | mvcapptemplate       | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp3.1;net5.0;net6.0;net7.0;net8.0;net9.0               | Windows;Linux      |
| SDK Web Large 3.0             | weblarge3.0          | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp3.1                             | Windows;Linux      |
| SDK Windows Forms Large       | windowsformslarge    | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp3.1                             | Windows            |
| SDK WPF Large                 | wpflarge             | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp3.1                             | Windows            |
| SDK Windows Forms Template    | windowsforms         | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp3.1                             | Windows            |
| SDK WPF Template              | wpf                  | pre.py default -f \<tfm> | test.py sdk \<build option> | post.py     | netcoreapp3.1                             | Windows            |
| SDK New Console               | emptyconsoletemplate | N/A                      | test.py sdk new_console     | post.py     | netcoreapp2.1;netcoreapp3.1;net5.0;net6.0;net7.0;net8.0;net9.0 | Windows;Linux      |
