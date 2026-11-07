# SDK Scenarios

An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](./scenarios-workflow.md). The current document has specific instruction to run:

- [SDK Build Throughput Scenario](#sdk-build-throughput-scenario)
- [Self-built MSBuild scenarios](#self-built-msbuild-scenarios)

## Self-built MSBuild scenarios

`eng\pipelines\msbuild-perf.yml` is a **manual-only, Windows/Linux x64** entrypoint
for dnceng/internal. It does not alter the existing SDK scenarios or activate
any pipeline. Register the YAML in the approved Azure Repos performance
repository. There is no schedule yet; the intended two-hour schedule remains
disabled until source/artifact access and a manual smoke run are qualified.

The default `accessCheckOnly: true` runs only the identity/source-access job.
It requires no SDK/asset URLs, asset credentials, Helix variable group, or
benchmark queue access. `Check-MSBuildSourceAccess.ps1` prints the authenticated
AzDO job identity name/ID and uses that job's `System.AccessToken` to probe
`DevDiv/DotNet-msbuild-Trusted` on `main`. It does not print the token, fall
back to developer Git credentials, download assets, build MSBuild, or submit
Helix work. A failed source probe fails the job explicitly; inspect the
preceding Git error to distinguish authorization from connectivity problems.
The printed identity is the current job principal, not a claim that a
cross-organization service connection already exists.

Select `accessCheckOnly: false` to include SDK composition and benchmarks.
That mode still needs source access, the pinned SDK/workload inputs below,
and authorization to use the existing `DotNet-HelixApi-Access` variable group
and Tiger queues. If the default dnceng job credential cannot read DevDiv,
an approved cross-organization credential/access arrangement must be wired
in before this mode can run; local developer access does not establish it.

The checked-in catalog ports **all 45 enabled PerfStar scenarios** across
12 versioned workload assets. Disabled PerfStar scenarios, desktop-only
workloads, and Framework `msbuild.exe` tests are intentionally excluded.
Canonical scenario names are retained; unsupported/disabled names fail
selection rather than being mapped to a rebuild.

The pipeline checks out the trusted Azure Repos MSBuild mirror, never GitHub,
and builds and packs `dotnet/msbuild` using the .NET build engine
in Release from `main` or a
full SHA reachable from the mirror's `main`. The latest mirror SHA is
resolved before fetch and checked out explicitly, then the six shipping packages are overlaid into
one checksum-pinned SDK. The selected SDK must match the current `main`
runtime band configured in `scripts\channel_map.py` and satisfy this
repository's `global.json`.
It uses the same DLL/target selection as PerfStar, but **not** official
signing, OptProf optimization, or publishing. Results are marked
`MSBuildSource=self-built-release`, with the MSBuild source SHA and commit
timestamp as their primary build identity.
The build job checks a package-free SDK project, not just `--version`,
before publishing composed inputs. This catches lazy assembly-binding
failures such as an incompatible `NuGet.Frameworks` version before the
benchmark matrix is submitted.

Supply an immutable Windows x64 SDK ZIP URL and its SHA-512 checksum, plus
`linuxSdkUrl` and `linuxSdkSha512` for a Linux x64 `.tar.gz` from the same
SDK release. `runLinux` defaults to true; set it false for Windows-only runs.
The Linux SDK uses the exact same self-built MSBuild shipping packages.
Safe in-archive file links are materialized during extraction, and executable
paths are recorded and restored when staging on Linux.

Supply `assetManifestUrl` and `assetManifestSha512` for a staged JSON
descriptor. Each selected workload needs its exact universal-package
version, an HTTPS archive URL, and a SHA-512 checksum. The descriptor uses
the following shape (replace placeholders and include every selected asset):

```json
{
  "assets": {
    "net8-console-app": {
      "version": "0.0.1",
      "url": "https://approved-host/immutable/Net8ConsoleApp.zip",
      "sha512": "<128 hexadecimal digits>"
    }
  }
}
```

| Asset/package | Version | Enabled operation families |
|---|---|---|
| `aspire` | `0.0.3` | rebuild, warm incremental build |
| `azure-core` | `0.0.1` | rebuild, warm incremental build |
| `eshop-on-blazor-modified` | `0.0.3` | rebuild |
| `net8-blazorwasm-app` (package `net8-blazor-wasm-app`) | `0.0.1` | rebuild |
| `net8-class-lib` | `0.0.1` | rebuild |
| `net8-console-app` | `0.0.1` | rebuild, `/nr:false`, warm incremental build, warm restore |
| `orchard-core` | `0.0.6` | rebuild, `/nr:false`, warm incremental build, cold/warm restore, evaluation Pack/Publish/project queries |
| `rar-search-stress` | `0.0.1` | rebuild |
| `rar-conflict-details-stress` | `0.0.1` | rebuild |
| `rar-conflict-warning-stress` | `0.0.1` | rebuild |
| `very-large-net8-console-app` | `0.0.1` | operational restore |
| `xunit` | `0.0.2` | rebuild, warm incremental build |

Archive paths, source edits, OS support, and exact iteration counts are in
`scripts\msbuild_scenarios.json`. The ZIP must be the archive inside the
universal package, not a ZIP of the universal-package staging directory.

Acquire those packages from `DevDiv/MSBuild.TestAssets` using an identity
with ReadPackages, then stage the ZIPs at an approved location accessible
to the pipeline. No feed publication or staging upload is performed by
this entrypoint. Checksums are mandatory; compute them with
`Get-FileHash -Algorithm SHA512`. Prefer credential-free archive URLs;
do not place access tokens in queue-time parameters.

For local composition, `--asset-manifest <file>` accepts the same descriptor
with a `path` instead of `url`; relative paths resolve beside the descriptor.
A remotely downloaded descriptor may not reference submitter-local paths.
The composed manifest stores relative archive paths so the Linux composition
can reuse the verified Windows downloads without downloading the assets again.
The old two-archive console/Orchard CLI options remain supported for local
prototype qualification.

The composed artifact includes the extracted SDK, both asset archives, and
a manifest containing SDK/overlay checksums, asset versions/checksums,
source SHA, source timestamp, and build configuration. It is shipped in
the existing Helix correlation payload rather than downloading a fresh SDK
on the worker.

Unlike PerfStar's daily-SDK path, composition does not rewrite targeting-pack
versions to a moving "latest published" version. Select a base SDK whose
targeting packs for these .NET 8 assets are available on the declared feeds.
Unavailable packs fail restore explicitly; any baseline patch must be made
before calculating and pinning the SDK archive checksum.

The default fan-out is **45 work items per OS**, using explicit `/mt:false`
or `/mt` together with `/m`. Use the pipeline's comma-separated `scenarios`
parameter to select exact canonical names. Unknown, disabled, unsupported,
duplicate, and missing-asset selections fail explicitly. `iterations: 0`
uses the original configured count for each scenario; a positive override
is intended for qualification and is recorded in report metadata.

Asset setup verifies and extracts each ZIP, declares internal-only NuGet
sources (including `.perfstar-packages`), and pins the composed SDK.
Preparation is not timed. Operations preserve these boundaries:

| Family | Setup and measured work |
|---|---|
| Rebuild | Initial restore and untimed build; shutdown before each `/t:rebuild`. CLI implicit restore remains enabled with warmed packages. |
| `/nr:false` rebuild | Same rebuild boundary, with the explicit node-reuse-disabled flag only on these two named scenarios. |
| Warm incremental build | Initial restore; per round shutdown, source edit, untimed priming build, another source edit, then measured build. |
| Cold/operational restore | Initial restore populates packages; delete `obj` outside timing and measure `/t:restore` from cold server state. |
| Warm restore | Initial restore; per round shutdown, untimed priming restore, then measured no-op restore with `obj` retained. |
| Orchard Pack/Publish evaluation | Restore and Release prebuild; untimed operation warm-up, then server-backed measured operations with shutdown and metrics completion per sample. Publish uses a sorted module/theme `.slnx` and Orchard's declared default framework. |
| Orchard project-query evaluation | Separate property and item query samples, alternating order across rounds. The primary wall-time counter is the property query; both variants retain independent evaluation counters. |

Node reuse/shared compilation remain enabled within a build except for the
two explicit `/nr:false` scenarios. Failed Linux MT rounds have a bounded
five-round recollection budget with discarded-attempt logging; persistent
build failures, shutdown failures, and malformed/incomplete metrics fail
qualification rather than producing success-shaped fallback samples.
Child tool lookup is pinned with both `PATH` and `DOTNET_HOST_PATH`, and
NuGet package/HTTP caches are isolated per work item. As in PerfStar,
`DOTNET_CLI_ENABLEAOT=0` keeps SDK commands on the managed path so native
CLI code cannot bypass the overlaid MSBuild. The RAR conflict-warning
workload retains its `/clp:ErrorsOnly` console-logging flag.
The worker rechecks the selected SDK's overlaid-file hashes before running.
On Windows it normalizes only future-dated SDK files before setup, avoiding
ZIP timezone artifacts that would invalidate warm incrementality. It does
not rewrite project source timestamps, and a system-wide `DOTNET_ROOT` is
rejected instead of being silently measured as the composed SDK.

Both workloads use monotonic wall-clock timing around the build process,
with no ETW, sampling profile, or binlog in measured iterations. The
existing Startup tool is used **parse-only** to produce standard PerfLab
reports (`MSBuild` category, `Build Wall Clock Time` in milliseconds).
This is not ETW Time on Thread or the existing SDK Clean Build metric.
Catalog defaults range from 1 to 12 iterations and use a 900-second timeout per command; failed
builds/timeouts fail the work item rather than becoming timing samples.
Helix work items have a six-hour budget.

For local use, compose an input directory with
`scripts\compose_msbuild_sdk.py --help`. Set `PERFLAB_MSBUILD_INPUTS` to that
directory, `DOTNET_ROOT` to its `sdk` subdirectory, and `PYTHONPATH` to this
repository's `scripts` and `src\scenarios` directories. From
`src\scenarios\msbuild`, with no existing `app`
directory, run:

```powershell
py -3 pre.py --scenario-id net8-console-app-warm-inc-build-mt-dotnet
py -3 test.py msbuild --scenario-id net8-console-app-warm-inc-build-mt-dotnet --iterations 1 --timeout 900
```

On Linux, use `python3`, a Linux composed SDK, and `:` rather than `;` when
setting `PYTHONPATH`. Normal timing requires no tracing privileges. Linux
SDK staging validates that the manifest platform matches the selected worker.

Select the exact MT/non-MT scenario identity rather than changing its flags.
Use `--diagnostics` for a separate, untimed repetition of the selected
operation with a binlog. Diagnostic failure also fails the work item. Existing
PerfLab environment variables are required to emit a JSON report locally.

### Evaluation metrics and catalog maintenance

The evaluation startup hook and lifecycle/session reader were ported from
PerfStar's `tests\EvaluationMetricsStartupHook\StartupHook.cs` and
`tests\Measurements\EvaluationMetricsSession.cs`. They are built in-repo;
runtime execution does not require a PerfStar checkout or Crank.
Hook/event contract checks, per-process completion, server identity, and
warm time-window validation are retained.

Canonical scenarios collect evaluation metrics by default. Ordinary
scenarios may explicitly use `--evaluation-collection off` for unsupported
historical builds; the three evaluation lighthouses require current
collection. A missing evaluation is not converted into zero: warm zeroes
require completed, capable collectors spanning the measurement window.
Both priming and measured windows are validated; only measured-window
evaluations contribute to warm counters.
The reader reports evaluation time/count, outside-build-submission
time/count, and complete per-pass families. Individual evaluation extrema
and their project/process/evaluation IDs are retained in
`traces\evaluation-extrema.json`.

Raw session files are written directly under `traces\.perfstar-evaluation-metrics`,
so lifecycle validation waits on the live collector directory rather than
an early copy that could miss late process-exit flushes.
The measurement envelope retains source clock frequency and relative
session paths so reports can be replayed after download or on another OS.
Incomplete cohorts, hook errors, and absent required server metrics fail
the report. Windows lifecycle replacement is serialized and retries
transient sharing/access conflicts at most five times (375 ms total);
retries are logged and exposed in the
`evaluation-collector-lifecycle-retries-count` counter. Persistent errors
are not suppressed. Measured builds remain untraced; the lightweight
evaluation hook is separate from optional binlog/collect-linux diagnostics.

To synchronize a later source inventory:

```powershell
py -3 scripts\sync_msbuild_scenarios.py --perfstar-root D:\git\dotnet-perfstar
py -3 scripts\sync_msbuild_scenarios.py --perfstar-root D:\git\dotnet-perfstar --check
```

The generator imports enabled rows only, preserving names, OS routing,
configured iterations, and pinned asset versions. It rejects unimplemented
enabled operations. Commit both generated outputs,
`scripts\msbuild_scenarios.json` and
`eng\performance\MSBuildScenarios.Generated.props`, with implementation
changes. The catalog records a platform-independent normalized inventory
SHA-256 for provenance. Collector code changes require separate review
and qualification; the catalog generator does not overwrite that code.
Curated project/edit bindings and workload-specific arguments in
`scripts\sync_msbuild_scenarios.py` must also be reviewed against the
upstream asset/test classes when those definitions change.

### SDK/workload compatibility

The port preserves the immutable workloads rather than suppressing SDK
errors or removing projects to force a successful build. Qualification
found that Aspire `0.0.3` fails static-web-assets endpoint validation with
base SDK `11.0.100-rc.2.26504.105`, including on a freshly extracted asset
with the untouched SDK and no metrics hook. The failing asset is
`Aspire.Managed` and the endpoint is the SDK's DotNetWatch JS module.

The older `11.0.100-rc.1.26425.128` SDK builds that asset without the overlay,
but is not a usable replacement for current MSBuild `fed363926dd715fb44d830e924c2e8820595c79e`:
the overlay requires `NuGet.Frameworks` 8.0, which that base SDK does not
provide. Select a compatible SDK/MSBuild/asset combination before including
Aspire in the first full run. Aspire remains registered and enabled; these
failures are not silently skipped or converted into timing samples.

### Linux diagnostic collection

The new MSBuild scenarios do not use or install `perfcollect`/LTTng. They
also do not remove existing LTTng kernel packages. Other, pre-existing
scenario types keep their existing behavior; this is not a repository-wide
tracing migration.

Use `--collect-linux` locally, or the pipeline's `collectLinux: true`, to
record **one separate diagnostic round of the selected operation**, after all untraced measurement
iterations. This also collects a binlog. Artifacts under `traces` include
`diagnostic.nettrace`, `diagnostic-0.binlog`, `diagnostic-0.log`, and
`collect-linux.log`; the existing Helix upload path preserves them.
No diagnostic rebuild duration is added to the primary timing samples.
Warm diagnostic rounds repeat their priming/edit boundaries; project-query
diagnostics capture both variants with separately suffixed binlogs and logs.

Collection uses `dotnet-trace collect-linux --profile dotnet-common,cpu-sampling`.
The version is pinned in `.config\dotnet-tools.json` (10.0.750501); pipeline
submission installs that version into the correlation payload only when
Linux diagnostics are requested. For local use, install the same version
and set `PERFLAB_DOTNET_TRACE` to the absolute executable path, or put
`dotnet-trace` on `PATH`.

**Use a dedicated worker.** Collection is machine-wide so MSBuild workers,
compiler servers, native code, and kernel CPU samples are included; it can
also capture unrelated activity. It requires Linux x64 with glibc 2.27+,
.NET 10+ for supported runtime events, kernel 6.4+ built with
`CONFIG_USER_EVENTS=y`, and mounted tracefs with `user_events_data`.
Run the collector as root or provision non-interactive `sudo` access.
The harness elevates only the collector, not measured builds. It does not
mount tracefs, change sysctls/capabilities, or silently fall back to
`perfcollect` or single-process EventPipe collection.

`linuxQueue` defaults to the existing `Ubuntu.2204.Amd64.Tiger.Perf` queue
for ordinary timing. **Do not assume that queue meets tracing prerequisites**:
select a dedicated, appropriately provisioned queue before enabling
`collectLinux`. Queue authorization and worker provisioning are not
performed by this change. The submitter uses `ubuntu-24.04`; that does
not determine the Helix worker's kernel.

Because `collect-linux` cannot launch the build command, the harness starts
it first, waits up to 60 seconds for its recording-ready message, then
starts the diagnostic rebuild. A terminal is used for the collector's
progress message even in CI. Completion sends SIGINT to finalize the trace,
with a finalization limit equal to the per-build timeout (900 seconds by
default). The recording-duration bound adds the 60-second startup allowance
so readiness time does not truncate a build near its timeout. Large builds can spend
several minutes resolving symbols after recording stops. Early collector exit, missing/empty output,
startup timeout, build failure, and finalization failure fail the work item
with logs retained. `--probe` alone is not treated as proof that kernel
collection works.

See the supported [collect-linux prerequisites and command reference](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-collect-linux).

ScenarioMeasurement's Startup/Util projects now reference TraceEvent 4.0.0.
Analyze these traces with a current reader that supports the .NET 11
EventPipe format and `Universal.Events`.
The pinned collector's own `report topN` command embeds an older reader
and can reject these traces with `Unknown label kind 10`; that command
is not used to generate the scenario's wall-clock report. Keep the
original `.nettrace` for analysis in a compatible reader.

Budget disk space and time for diagnostics separately from timing runs.
The validated Orchard capture produced approximately 1.6 GiB and needed
several minutes of symbol finalization. Collection remains opt-in; it is
not appropriate to enable for every measurement iteration.

Per-commit triggering is deliberately deferred. A GitHub secondary
repository resource cannot provide a native Azure Pipelines repository
push trigger. Enabling that later requires an MSBuild-repository entrypoint
or another explicitly authorized trigger mechanism; adding
`trigger: main` here would only watch the performance repository.

## SDK performance pipeline

The repository's [azure-pipelines.yml](../azure-pipelines.yml) entrypoint uses
[sdk-perf-jobs.yml](../eng/pipelines/sdk-perf-jobs.yml) for SDK-based scenario and
benchmark jobs. It backs the internal `dotnet-performance` pipeline (definition 306)
and the public `performance-ci` pipeline (definition 38).

SDK jobs no longer invoke the Mono-based Blazor scenarios (including scheduled
size-on-disk measurements) or MAUI Android/iOS Mono smoke and .NET 10 baseline
scenarios. CoreCLR and NativeAOT jobs keep their existing channels and settings.
The scenario projects and manual Mono execution support remain available.

The entrypoint always enables asynchronous submission and includes the standalone
Helix Job Monitor, its pool provider, and its parameters, including on public and PR
runs. The monitor runs alongside submitters in the same implicit stage, waits for
their Helix work items, and reports failures to Azure DevOps. The existing manual
job-selection parameters and schedules are unchanged: public runs always select
correctness jobs. Internal manual runs must select workloads; leaving all three
job-selection flags false still includes the monitor and fails because no Helix jobs
were submitted (`allowNoHelixJobs: false`). Submitter and work-item failures also
continue to fail the run.

The monitor job has a six-hour timeout (355 minutes for the tool, leaving five minutes
for it to exit). Existing 320-minute submitter limits and Helix work-item timeouts are
unchanged. These limits start when the respective job runs; they are not a whole-run
wall-clock deadline that includes time waiting for agents.

Internal runs, including internal PR runs, import `DotNet-HelixApi-Access`. The
monitor forwards `$(HelixApiAccessToken)` directly, matching the shared pipeline
convention. Internal runs use the imported credential. Public runs, including public
PR runs, do not import that group and use public Helix access. Direct forwarding
does not change how Azure DevOps expands variables or how the SDK handles tokens.
Azure DevOps timeline access and result reporting still use the job's
`System.AccessToken`. The tool is restored from
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

## Blazor workload preparation

SDK Blazor jobs install `wasm-tools` once on the build agent through
`PreparePayloadWorkItems`, using the SDK in the correlation payload, its bundled
manifests (`--skip-manifest-update`), and the payload's `NuGet.config`. An installation
failure stops payload preparation before the job is sent to Helix.

Helix work items use `pre.py publish --has-workload --readonly-dotnet` and
`post.py --readonly-dotnet`. These flags prevent the scenario helpers from changing
the shared workload installation; they do not set filesystem permissions. Application
restore, publish, size measurements, and cleanup of work-item-local outputs still run
on Helix. Standalone scenario scripts retain their existing workload-management
defaults.

Workload availability does not select the benchmark's compilation mode. The Minimum
Template explicitly enables native relinking, while the non-AOT New Template, Pizza,
and Localized scenarios disable it with `WasmNativeWorkload=false`. AOT scenarios
enable `RunAOTCompilation=true`. The Localized AOT command passes this property directly
instead of referencing item metadata as an MSBuild property, which previously dropped
the AOT argument. Its reported sizes may therefore change when it first runs with AOT
actually enabled.

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
