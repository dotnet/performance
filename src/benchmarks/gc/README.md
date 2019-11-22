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



# Setup

### Install python 3.7+

On Windows, just go to https://www.python.org/downloads/ and run the installer.
On other systems it’s better to use your system’s package manager.


### Install Python dependencies

```sh
py -m pip install -r src/requirements.txt
```

You will likely run into trouble installing pythonnet.

First, pythonnet is only needed to analyze test results, not to run tests.
If you just want to run tests on this machine, you could comment out pythonnet from `src/requirements.txt`.
Then when running tests, provide the `--no-check-runs` option.


#### Pythonnet on Windows

On Windows, if you run into trouble installing pythonnet, look for an error like:

    Cannot find the specified version of msbuild: '14' 

or:

    Could not load file or assembly 'Microsoft.Build.Utilities, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

If so, you may need to install Visual Studio 2015.


#### Pythonnet on other systems

Pythonnet [does not work](https://github.com/pythonnet/pythonnet/issues/939) with the latest version of mono, so you'll need to downgrade that to version 5.

On Ubuntu the instructions are:

* Change `/etc/apt/sources.list.d/mono-official-stable.list` to:
```
deb https://download.mono-project.com/repo/ubuntu stable-bionic/snapshots/5.20.1 main
```
* `sudo apt remove mono-complete`
* `sudo apt update`
* `sudo apt autoremove`
* `sudo apt install mono-complete`
* `mono --version`, should be 5.20.1

Then to install from source:

* Instructions: https://github.com/pythonnet/pythonnet/wiki/Installation
* `py setup.py bdist_wheel --xplat`
* WARN: The instructions there tell you to run `pip install --no-index --find-links=.\dist\ pythonnet`.
  This may "succeed" saying `Requirement already satisfied: pythonnet in /path/to/pythonnet`.
  INSTEAD, go to the *parent* directory and use `sudo python3.7 -m pip install --no-index --find-links=./pythonnet/dist/` which circumvents this bug.
* Run `import clr` in the python interpreter to verify that installation worked.


If you see an error:
```
fatal error: Python.h: No such file or directory
```

You likely have python installed but not dev tools. See https://stackoverflow.com/questions/21530577/fatal-error-python-h-no-such-file-or-directory .

### Building C# dependencies

Navigate to `src/exec/GCPerfSim` and run `dotnet build -c release`.

Navigate to `src/analysis/managed-lib` and run `dotnet publish`.



### Windows-Only Building

Open a Visual Studio Developer Command Prompt, go to `src/exec/env`, and run `.\build.cmd`.
This requires `cmake` to be installed.




### Other setup

You should have `dotnet` installed.
On non-Windows systems, you'll need [`dotnet-trace`](https://github.com/dotnet/diagnostics/blob/master/documentation/dotnet-trace-instructions.md) to generate trace files from tests.
On non-Windows systems, to run container tests, you'll need `cgroup-tools` installed.
You should have builds of coreclr available for use in the next step.

Finally, run `py . setup` from the same directory as this README.
This will read information about your system that's relevant to performance analysis (such as cache sizes) and save to `bench/host_info.yaml`.
It will also install some necessary dependencies on Windows.




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

`suite-create` generates a set of default tests as different `.yaml` files, and a `suite.yaml` file referencing them. 


## Running

Running the full suite would take a while, so let's just run one:

```sh
py . run bench/suite/low_memory_container.yaml
```

This test must be run as super user (or administrator on Windows).
Super user is because we are creating a container for this particular test; tests without a container don't need super user privelages.

You might get errors due to `dotnet` or `dotnet-trace` not being found. Or you might see an error:

```
A fatal error occurred. The required library libhostfxr.so could not be found.
```

Or:

```
A fatal error occurred, the default install location cannot be obtained.
```

To fix either of these, specify `dotnet_path` and `dotnet_trace_path` in `options:` in the benchfile. (Use `which dotnet` and `which dotnet-trace` to get these values.)

On Windows, all tests must be run as administrator as PerfView requires this.
(Unless `collect: none` is set the benchfile's options. See [Running Without Traces](#Running Without Traces).)

(Note that if you recently built coreclr, that probably left a `dotnet` process open that `run` will ask you to kill. Just do so and run again with `--overwrite`.)

This simple test should take under 2 minutes. Other tests require more patience.
We aim for an individual test to take about 20 seconds and this does 2 iterations for each of the 2 coreclrs.

Running the test produced a directory `bench/suite/low_memory_container.yaml.out`.
This contains a trace file (and some other small files) for each of the tests. (If you had specified `collect: none` in `options:` in the benchfile, there would be no trace file and the other files would contain all information.)
Each trace file can be opened in PerfView if you need to.

Each trace file will be named `{coreclr_name}__{config_name}__{benchmark_name}__{iteration}`, e.g.  `clr_a__smaller__nosurvive__0`.


## Analyzing

Now let's analyze the results.

```sh
py . diff bench/suite/low_memory_container.yaml
```

(Like most commands operating on the output of the benchfile,
this take the benchfile as input, not the `.out` directory.)

This produces something like:

```
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



## Conclusion

Now you know how to create, run, and analyze a test.

In many cases, all you need to use the infra is to manually modify a benchfile, then `run` and `diff` it.


# Metrics

Analysis commands are based on metrics.

A metric is the name of a measurement we might take. The 'metric' is the *name* of the measurement, not the metric itself. Length is a metric, 3 meters is a 'metric value'.

A run-metric is the name a measurement of some property of an entire run of a test. For example, `FirstToLastGCSeconds` is the metric that measures the time a test took. Another example is `PauseDurationMSec_Mean` which is the mean pause duration of a GC. (Since getting the average requires looking at every GC, it is considered a metric of the whole run, not a single-gc-metric.)

A single-gc-metric is the name of a measurement of some property of a single GC within a test. For example, `PauseDurationMSec` measures the time of that individual GC (and as we've seen, we can add `_Mean` to get a run-metric.)

A single-heap-metric is the name of a measurement of some property of a single heap within a single GC. (This applies to the 'server' GC mode which has multiple heaps.)

You can see all available metrics [here](docs/metrics.md).

Most analysis commands require you to specify the metrics you want (although many provide defaults). The simplest example is `analyze-single` which can take a single trace and print out metrics.

```
py . analyze-single bench/suite/low_memory_container.yaml.out/a__only_config__tlgb0.2__0.etl --run-metrics FirstToLastGCSeconds --single-gc-metrics DurationMSec --single-heap-metrics InMB OutMB
```

The output will look like:

```
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


AMD64 is not currently supported.
The `affinitize`  and `memory_load_percent` properties of a benchfile's config are not yet implemented outside of Windows.


# Further Reading

See [example](docs/example.md) for a more detailed example involving more commands.

Use `py . help` to see all commands.
Also see the `docs` directory for other topics, especially [commands syntax](docs/commands syntax.md).

Before modifying benchfiles, you should read [bench_file](docs/bench_file.md) which lists everything you can specify in a benchfile.

Commands can be run in a Jupyter notebook instead of on the command line. See [jupyter notebook](docs/jupyter notebook.md).




# Terms

### Metric

The name of a measurement we might take.
See more in `docs/metrics.md`.


### Benchfile

A YAML file that describes the benchmarks to run and the configs (environment variables and container) and coreclr versions to run them under.

### Core_Root

This is the build output of coreclr that is used to run benchmarks.

These are specified in the `coreclrs` section of a benchfile.

This can be found in a directory like `bin/tests/Windows_NT.x64.Release/Tests/Core_Root` (adjust for different OS or architecture) of a coreclr repository. (The Core_Root can be moved anywhere and doesn't need to remain inside the coreclr repository.)

A clone of https://github.com/dotnet/coreclr,  which may be on an arbitrary commit (including one not checked in).
When you make a change to coreclr, you will generally make two clones, one at master and one at your branch (which may be on your fork).
Alternately, you may have only one checkout, build multiple times, copy the builds to somewhere, and specify coreclrs using `core_root` instead of `path`.


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
