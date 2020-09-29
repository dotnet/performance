# About

This program lets you run GC performance tests and analyze and chart statistics.

Command examples in this document use Bash/PowerShell syntax. If using Window's CMD, replace `/` with `\`.

The general workflow when using the GC infra is:

* For testing your changes to coreclr, get a master branch build of coreclr, and also your own build.
  (You can of course use any version of coreclr, not just master.
  You can also only test with a single coreclr.)
* Write a benchfile. (Or generate default ones with `suite-create` as in the tutorial.) This will reference the coreclrs and list the tests to be run.
* Run the benchfile and collect traces.
* Run analysis on the output.

NOTE: If running under ARM/ARM64, the program's functionalities are limited to running certain benchmarks only and the setup process is slightly different. This is pointed out as necessary throughout this document. Look out for the _ARM NOTE_ labels. As for the other necessary tools without official ARM/ARM64 downloads (e.g. python, cmake), you can install and run the x86 versions.

# Setup

### Install python 3.7+

You will need at least version 3.7 of Python.
WARN: Python 3.8.0 is [not compatible](https://github.com/jupyter/notebook/issues/4613) with Jupyter Notebook on Windows.
This should be fixed in 3.8.1.

On Windows, just go to https://www.python.org/downloads/ and run the installer.
It's recommended to install a 64-bit version if possible, but not required.

On other systems, it’s better to use your system’s package manager.

### Install Python dependencies

```sh
py -m pip install -r src/requirements.txt
```

### Install pythonnet

Pythonnet is only needed to analyze test results, not to run tests.
If you just want to run tests on this machine, you can skip installing pythonnet,
copy bench output to a different machine, and do analysis there.

For instructions to install pythonnet, see [docs/pythonnet.md](docs/pythonnet.md).

### Building C# dependencies

Navigate to `src/exec/GCPerfSim` and run `dotnet build -c release`.
This builds the default test benchmark. (You can use other benchmarks if you want, in which case this does not need to be built.)

Navigate to `src/analysis/managed-lib` and run `dotnet publish`.
This builds the C# library needed to read trace files. Python will load in this library and make calls to it.
This intentionally uses a debug build to have added safety checks in the form of assertions.

### Windows-Only Building

Open a Visual Studio Developer Command Prompt, go to `src/exec/env`, and run `.\build.cmd`.
This requires `cmake` to be installed.

_ARM NOTE_: Skip this step. Visual Studio and its build tools are not supported on ARM/ARM64.

### Other setup

You should have `dotnet` installed.
On non-Windows systems, you'll need [`dotnet-trace`](https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md) to generate trace files from tests.
On non-Windows systems, to run container tests, you'll need `cgroup-tools` installed.
You should have builds of coreclr available for use in the next step.

Finally, run `py . setup` from the same directory as this README.
This will read information about your system that's relevant to performance analysis (such as cache sizes) and save to `bench/host_info.yaml`.
It will also install some necessary dependencies on Windows.

_ARM NOTE_: Since build tools do not work on ARM/ARM64, `py . setup` will automatically skip reading and writing the system's information. You will have to get the machine's specs and write `bench/host_info.yaml` manually. This file follows the format shown below (it might vary depending on your machine's NUMA nodes and caches).

```yaml
hostname:
n_physical_processors:
n_logical_processors:
numa_nodes:
- numa_node_number:
  ranges:
  - lo:
    hi:
  cpu_group_number:
cache_info:
  l1:
    n_caches:
    total_bytes:
  l2:
    n_caches:
    total_bytes:
  l3:
    n_caches:
    total_bytes:
clock_ghz:
total_physical_memory_mb:
```

Most (if not all) of these fields can be retrieved from your machine's _Task Manager_ and under _System_ within _Control Panel_.

# Tutorial

## Specifying tests/builds

You should have run `py . setup` already.

You can write a *benchfile* to specify tests to be run. You then run these to create *tracefiles*.
You then analyze the trace files to produce a result.

The benchfiles can exist anywhere. This example will use the local directory `bench` which is in `.gitignore` so you can use it for scratch.

To avoid writing benchfiles yourself, `suite-create` can generate a few:

```sh
py . suite-create bench/suite --coreclrs path_to_coreclr0 path_to_coreclr1
```

`path_to_coreclr0` is the path to a [Core_Root](#Core_Root).

`path_to_coreclr1` should be a different Core_Root. (It can be the same, but the point is to compare performance of two different builds.)
You can omit this if you just intend to test a single coreclr.

If you made a mistake, you can run `suite-create` again and pass `--overwrite`, which clears the output directory (`bench/suite` in this example) first.

The `suite-create` command generates a set of default scenarios as different `.yaml` files,
which specify a set of tests, and a `suite.yaml` file referencing these scenarios.

Each test `.yaml` file looks something like the example described below:

```yaml
vary: coreclr
test_executables:
  defgcperfsim: <path to the GCPerfSim dll built in the earlier steps>
coreclrs:
  a:
    core_root: <path to first CoreCLR core root>
  b:
    core_root: <path to second CoreCLR core root>
options:
   <option configuration such as timeouts, number of iterations, etc>
common_config:
   <configuration values such as number of heaps, concurrent gcs, etc>
benchmarks:
  0gb:
    arguments:
      tc: 10
      tagb: 300
      tlgb: 0
      <other parameters for GCPerfSim>
  <other benchmark tests to run>
```

The configuration values that can be used in the test `.yaml` file are described under [docs/bench_file.md](docs/bench_file.md).

## Running

The benchmarking scenarios created in the previous step can be run as a whole bundle, or individually.
This is explained in the following sections.

### Running the Entire Suite

To run all the tests at once, you ask the infra to perform a *suite-run*. This
functionality also allows you to run as many scenarios/tests as you'd like in a
bundle, which you specify in the suite yaml file (more information on this later).

The command to run this is the following:

```sh
py . suite-run bench/suite/suite.yaml
```

The `suite.yaml` file contains a list of all the scenarios you wish to run in the
following format:

```yml
bench_files:
- normal_workstation.yaml
- normal_server.yaml
- high_memory.yaml
- low_memory_container.yaml
command_groups: {}
```

GC Benchmarking Infra will read one by one each of the specified files under *bench_files*,
and run their specified executables accordingly. If any test fails, Infra will
proceed to run the next one and will display a summary of the encountered problems
at the end of the run.

The *command_groups* tag is used to store sets of other commands you might want to run in bulk,
rather than individually. For simplicity, it is left empty in this example.

When *GCPerfSim* is modified, it is important to run the full suite of default
scenarios with both, the original and the modified versions of *GCPerfSim*. You
only need to make sure to keep a copy before rebuilding it, and then specify
both dll's under the `test_executables` group in the `yaml` file. This is to
ensure no regressions have occurred and the tool continues to work properly.

For full information regarding suites, check the full documentation [here](docs/suites.md).

### Running a Single Scenario

Let's run *low_memory_container* for this example.

```sh
py . run bench/suite/low_memory_container.yaml
```

On Windows, all tests must be run as administrator as PerfView requires this,
unless `collect: none` is set the benchfile's options. See [Running Without Traces](#Running%20Without%20Traces)
for more details.

On Linux, only tests with containers require super user privileges.

You might get errors due to `dotnet` or `dotnet-trace` not being found. Or you might see an error:

```
A fatal error occurred. The required library libhostfxr.so could not be found.
```

Or:

```
A fatal error occurred, the default install location cannot be obtained.
```

To fix either of these, specify `dotnet_path` and `dotnet_trace_path` in `options:` in the benchfile. (Use `which dotnet` and `which dotnet-trace` to get these values.)

Note that if you recently built coreclr, that probably left a `dotnet` process open that `run` will ask you to kill. Just do so and run again with `--overwrite`.

This simple scenario should take under 2 minutes. Other ones require more time.
We aim for an individual test to take about 20 seconds and this does 2 iterations for each of the 2 *coreclrs*.

Running this produced a directory called `bench/suite/low_memory_container.yaml.out`.
This contains a trace file (and some other small files) for each of the tests. (If you had specified `collect: none` in `options:` in the benchfile, there would be no trace file and the other files would contain all information.)
Each trace file can be opened in PerfView if you need to.

Each trace file will be named `{executable_name}__{coreclr_name}__{config_name}__{benchmark_name}__{iteration}`, e.g.  `defgcperfsim__clr_a__smaller__nosurvive__0`.

_ARM NOTE_: Container tests and high memory loading tests are not supported on ARM/ARM64.

### Running with .NET Desktop

Now, it is also possible to run benchmarks using _.NET Desktop_ aside from _.NET Core_. In order to do this, we use a _self contained_ executable of _GCPerfSim_, built targeting the desktop .NET Framework. The steps to do this are described below.

First, we need to tell `dotnet` to build _GCPerfSim_. Navigate to `src/exec/GCPerfSim` and open `GCPerfSim.csproj`.

Within the _TargetFrameworks_ property, add the .NET Desktop version you want to build for.

```xml
<TargetFrameworks>net472;netcoreapp2.2;netcoreapp3.0;netcoreapp3.1;netcoreapp5.0</TargetFrameworks>
```

In this example, we are adding version 4.7.2 to the already existing ones of .NET Core.

Next, you have to rebuild the binaries like before. Issue `dotnet build -c release` again. This will generate the new _self contained_ executable of _GCPerfSim_ back in the `artifacts` directory path at the root of the _performance_ repo. By default, it is located in `performance/artifacts/bin/GCPerfSim/release/net472/GCPerfSim.exe`, supposing you are targeting 4.7.2 as in this example. Otherwise, replace that folder with the version you selected.

After this is completed, we will tell our _bench_ files to use this executable. Open your favorite one in your favorite editor.

There, under the `coreclrs` section, replace the `core_root` property with `self_contained` set to `true`.

On each benchmark, add an `executable` property before the `arguments` one and set it to the path of your newly built `GCPerfSim.exe`.

For example, your benchmark file could end up looking something like the following:

```yaml
coreclrs:
  a:
    self_contained: true

<other configuration values>

benchmarks:
  0gb:
    executable: performance/artifacts/bin/GCPerfSim/release/net472/GCPerfSim.exe
    arguments:
      tc: 10
      <other benchmark values>

  2gb:
    executable: performance/artifacts/bin/GCPerfSim/release/net472/GCPerfSim.exe
    arguments:
      tc: 10
      <other benchmark values>

<remaining benchmarks to be run>
```

Finally, you are ready to run your tests as explained in the previous **Running** section.

## Test status files

Each trace has (at least) two files associated with it: A `.etl` (or also could be`.etlx`,
`.btl`, `.nettrace`), and a `.yaml`. This last one is called a test status file,
which provides information about the process to focus on, among other things.

A minimal test status file would look like this:

```yml
    # this file: `x.yaml`
    success: true
    trace_file_name: x.etl
    process_id: 1234
```

In this example, we are using `process_id` to identify the process we want to analyze.
However, you can give the `process_name` and/or `process_args` instead. Only
these 3 lines are required, but for more information and a full specification,
check the detailed documentation found [here](docs/test_status_files.md).

You can write these files by hand for traces you got from elsewhere.

## Analyzing

Now let's analyze the results.

```sh
py . diff bench/suite/low_memory_container.yaml
```

Like most commands operating on the output of the benchfile,
this takes the benchfile as input, not the `.out` directory.

This produces something like:

```text
                         ┌────────────────────────────┐
                         │ Summary of important stats │
                         └────────────────────────────┘


                                   │    PctTimePausedInGC │ FirstToLastGCSeconds
                              name │ Base │  New │ % Diff │ Base │  New │ % Diff
───────────────────────────────────┼──────┼──────┼────────┼──────┼──────┼───────
DESKTOP-FD1M5BH__only_config__tlgb │ 64.6 │ 64.3 │ -0.473 │ 12.6 │ 13.5 │   7.07
0.2                                │      │      │        │      │      │



                                  │ HeapSizeBeforeMB_Mean │ HeapSizeAfterMB_Mean
                             name │ Base │ New │   % Diff │ Base │ New │  % Diff
──────────────────────────────────┼──────┼─────┼──────────┼──────┼─────┼────────
DESKTOP-FD1M5BH__only_config__tlg │  442 │ 448 │     1.50 │  441 │ 448 │    1.56
b0.2                              │      │     │          │      │     │



                │ PauseDurationMSec_95PWhereIsG │ PauseDurationMSec_95PWhereIsGe
                │                           en0 │                             n1
           name │ Base │  New │          % Diff │ Base │  New │           % Diff
────────────────┼──────┼──────┼─────────────────┼──────┼──────┼─────────────────
DESKTOP-FD1M5BH │ 5.68 │ 6.97 │            22.8 │ 4.57 │ 4.59 │            0.499
__only_config__ │      │      │                 │      │      │
tlgb0.2         │      │      │                 │      │      │



                │ PauseDurationMSec_95PWhereIsB │ PauseDurationMSec_95PWhereIsBl
                │                     ackground │                     ockingGen2
           name │ Base │  New │          % Diff │ Base │  New │           % Diff
────────────────┼──────┼──────┼─────────────────┼──────┼──────┼─────────────────
DESKTOP-FD1M5BH │ 2.42 │ 1.64 │           -32.0 │ 69.7 │ 45.6 │            -34.5
__only_config__ │      │      │                 │      │      │
tlgb0.2         │      │      │                 │      │      │

```

This is followed by a list of each metric, sorted by how significantly it differed.

In this case, all diffs should tend toward 0 since we're testing on two identical coreclrs.
`95P` metrics tend to have high standard deviation, since we are only considering the worst instances.

_ARM NOTE_: There is no support to analyze benchmark results on ARM/ARM64. In order to use these results, you will need to transfer them to another machine and perform the analysis there.

## CPU Samples Analysis

GC Benchmarking Infrastructure also supports analyzing CPU Samples from traces.
This is only supported on Jupyter Notebook and you can find the full instructions
on how to run it [here](docs/jupyter%20notebook.md).

## Conclusion

Now you know how to create, run, and analyze a test.

In many cases, all you need to use the infra is to manually modify a benchfile, then `run` and `diff` it.

# Metrics

Analysis commands are based on metrics.

A metric is the name of a measurement we might take. The 'metric' is the *name* of the measurement, not the metric itself. Length is a metric, 3 meters is a 'metric value'.

A run-metric is the name a measurement of some property of an entire run of a test. For example, `FirstToLastGCSeconds` is the metric that measures the time a test took. Another example is `PauseDurationMSec_Mean` which is the mean pause duration of a GC. Since getting the average requires looking at every GC, it is considered a metric of the whole run, not a single-gc-metric.

A single-gc-metric is the name of a measurement of some property of a single GC within a test. For example, `PauseDurationMSec` measures the time of that individual GC (and as we've seen, we can add `_Mean` to get a run-metric.)

A single-heap-metric is the name of a measurement of some property of a single heap within a single GC. (This applies to the 'server' GC mode which has multiple heaps.)

You can see all available metrics [here](docs/metrics.md).

Most analysis commands require you to specify the metrics you want (although many provide defaults). The simplest example is `analyze-single` which can take a single trace and print out metrics.

```sh
py . analyze-single bench/suite/low_memory_container.yaml.out/a__only_config__tlgb0.2__0.etl --run-metrics FirstToLastGCSeconds --single-gc-metrics DurationMSec --single-heap-metrics InMB OutMB
```

The output will look like:

```text
                  ┌─────────────────┐
                  │ Overall metrics │
                  └─────────────────┘


                                              Name │ Value
  ─────────────────────────────────────────────────┼──────
                              FirstToLastGCSeconds │  12.6
  ─────────────────────────────────────────────────┼──────
                             PauseDurationMSec_95P │  6.85
  ─────────────────────────────────────────────────┼──────
                                             speed │  9.28
  ─────────────────────────────────────────────────┼──────
  Gen2ObjSpaceBeforeMB_Sum_MeanWhereIsBlockingGen2 │   343
  ─────────────────────────────────────────────────┼──────
    Gen2ObjSizeAfterMB_Sum_MeanWhereIsBlockingGen2 │   198
  ─────────────────────────────────────────────────┼──────
                                             space │ 0.760



      ┌───────────────────────┐
      │ Single gcs (first 10) │
      └───────────────────────┘


  gc number │ Generation │ DurationMSec
  ──────────┼────────────┼─────────────
          3 │          0 │         5.65
  ──────────┼────────────┼─────────────
          4 │          0 │         5.71
  ──────────┼────────────┼─────────────
          5 │          2 │         72.0
  ──────────┼────────────┼─────────────
          6 │          0 │         2.76
  ──────────┼────────────┼─────────────
          7 │          1 │         30.4
  ──────────┼────────────┼─────────────
          8 │          0 │         2.51
  ──────────┼────────────┼─────────────
          9 │          0 │         2.26
  ──────────┼────────────┼─────────────
         10 │          1 │         4.13
  ──────────┼────────────┼─────────────
         11 │          0 │         2.18
  ──────────┼────────────┼─────────────
         12 │          0 │         1.45



     ┌──────┐
     │ GC 3 │
     └──────┘


  heap │ InMB │ OutMB
  ─────┼──────┼──────
     0 │ 10.0 │  10.0
  ─────┼──────┼──────
     1 │ 9.58 │  9.58
  ─────┼──────┼──────
     2 │ 10.1 │  10.1
  ─────┼──────┼──────
     3 │ 9.96 │  9.96
  ─────┼──────┼──────
     4 │ 10.5 │  10.5
  ─────┼──────┼──────
     5 │ 9.84 │  9.84
  ─────┼──────┼──────
     6 │ 9.54 │  9.54
  ─────┼──────┼──────
     7 │ 10.2 │  10.2
...
```

As you can see, the run-metrics appear only once for the whole trace, the single-gc-metrics have different values for each GC, and the single-heap-metrics have a different value for each different heap in each GC.

# GCPerfSim

Although benchmarks can run any executable, they will usually run GCPerfSim. You can read its documentation in the [source](src/exec/GCPerfSim/GCPerfSim.cs).

# Running Without Traces

Normally tests are run while collecting events for advanced analysis.

If you set `collect: none` in the `options` section of your [benchfile](docs/bench_file.md) (if it doesn't exist yet you can add it at the top-level), collection will be disabled.

If you don't have a trace, you are limited in the metrics you can use. No single-heap or single-gc-metrics are available since individual GCs aren't collected. However, GCPerfSim outputs information at the end which is stored in the test status file (a `.yaml` file with the same name as the trace file would have). You can view those metrics in the section "float metrics that only require test status" [here](docs/metrics.md).

# Limitations

* ARM/ARM64 are only supported to run basic tests (See above for further details).
* The `affinitize` and `memory_load_percent` properties of a benchfile's config are not yet implemented outside of Windows.

# Further Reading

See [example](docs/example.md) for a more detailed example involving more commands.

Use `py . help` to see all commands.
Also see the `docs` directory for other topics, especially [commands syntax](docs/commands%20syntax.md).

Before modifying benchfiles, you should read [bench_file](docs/bench_file.md) which lists everything you can specify in a benchfile.

Commands can be run in a Jupyter notebook instead of on the command line. See [jupyter notebook](docs/jupyter%20notebook.md).

# Terms

### Metric

The name of a measurement we might take.
See more in the [metrics doc](docs/metrics.md).

### Benchfile

A YAML file that describes the benchmarks to run and the configs (environment variables and container) and coreclr versions to run them under.

### Core_Root

This is the build output of coreclr that is used to run benchmarks. These are specified in the `coreclrs` section of a benchfile.

Since the codebase of coreclr was moved to the runtime repo, the way of generating the `core_root` has changed.
See [docs/building_coreroot.md](docs/building_coreroot.md) for detailed instructions on how to get it.

### Config

Environment in which coreclr will be invoked on a benchmark. This includes environment variables that determine GC settings, as well as options for putting the test in a container.
See `docs/bench_file.md` in the `## Config` section for more info.

### Benchmark

Path to a managed DLL (usually GCPerfSim) and its command-line arguments.
A benchmark alone does not specify what coreclr or config to run it with.

The recorded events of a test run.
May be an ETL or netperf file.
ETL files come from using PerfView to collect ETW events, which is the default on Windows.
Netperf files come from using dotnet-trace, which uses EventPipe. This is the only option on non-Windows systems.

# Contributing

See [contributing](docs/contributing.md).
