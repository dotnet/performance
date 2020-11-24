# Using with Jupyter Notebook

A jupyter notebook has already been set up in `jupyter_notebook.py`. So far,
it's only been tested with VSCode.

## Using with VSCode

* Run `code .` in the `/performance/src/benchmarks/gc` directory.
* Open `jupyter_notebook.py`.
* Open your settings and enable `"editor.codeLens": true,`.
* Wait a minute for CodeLens to show up in the notebook.

## Overview

* Click on `Run cell` in the top cell. This is the only cell that is not optional to run.
* Each of the other cells corresponds to some command. Instead of providing command
  line arguments, edit the code to provide different arguments to the function.
  You can then re-run that cell without needing to reload traces.
* The top cell specifies all the trace files and metrics that will be available.
  If you need to add new files or metrics you will need to rerun the top cell.
* If you edit code in any other file you will have to reload the whole notebook.
  See the ⟲ icon in the top-right.
* You can also do any custom analysis on the trace. The `custom` section at the
  bottom shows how to manually sum all GC times.

## CPU Samples Analysis

CPU Samples Analysis is a set of features in active development. It is currently
only usable through Jupyter Notebook, as will be described in this section.

The following functionalities are supported:

* Chart CPU Samples metrics of one or more functions, for one or more GC's,
  given one or more traces. A list of supported metrics is described later on.
* Show various CPU Samples metrics, such as inclusive and exclusive sample count,
  for a function within a given time range.

### Requirements and Setup
 
To begin with, capture a trace with CPU Samples enabled. If you use GC Infra, then
you're good to go. If you capture it elsewhere, make sure to include the `process_name`
and the `seconds_taken` fields in the [test status yaml file](test_status_files.md).

You will also need to provide the path to where you have your _Core\_Root's_ PDB's
with the symbols. If you built it from the runtime repo as specified [here](building_coreroot.md),
they should be in the following path (assuming you built for Windows x64):

```sh
/runtime/artifacts/bin/coreclr/Windows_NT.x64.Release/PDB/
```

Once the previous requirements are fulfilled, open the _Jupyter Notebook_ in VS Code.
There, make sure to run the first cell, which is in charge of setting up and loading
all the libraries and components required for analysis. Then, read your trace
with the following code:

```python
_MY_TRACE = get_trace_with_everything("Path/To/Test/Status/File.yaml")
```

Next, you need to create an _"utilities"_ object associated with your trace.
This object will create and store for you the necessary components used by
GCPerf and TraceEvent to read and analyze the trace as you require. Build it
with the following code:

```python
_MY_TRACE_ALL_DATA = TraceReadAndParseUtils(
    ptrace=_MY_TRACE,
    symbol_path=Path("Path/To/PDB/Directory"),
)
```

Now you can choose between the two functionalities mentioned above. But before
that, don't forget to import their respective functions:

```python
from src.analysis.analyze_cpu_samples import chart_cpu_samples_per_gcs, show_cpu_samples_metrics
```

### Charting CPU Samples per GC's

The following function will create a chart from the given CPU Samples metric from
the given function(s) per individual GC's. You can filter those to a certain criteria
as well (e.g. Gen1 GC's only). Also, you can provide more than one trace for a
more complex comparison.

The currently supported metrics to chart from a given function are the following:

* Inclusive Number of CPU Samples _("inclusive\_count")_
* Exclusive Number of CPU Samples _("exclusive\_count")_
* Inclusive Percent of CPU Samples _("inclusive\_metric\_percent")_
* Exclusive Percent of CPU Samples _("exclusive\_metric\_percent")_
* Time of First Sample _("first\_time\_msec")_
* Time of Last Sample _("last\_time\_msec")_

In this example, we will be charting the _inclusive number of samples_ from the
functions `gc_heap::plan_phase` and `gc_heap::mark_phase`, for all Generation 1 GC's.

```python
chart_cpu_samples_per_gcs(
    ptraces_utils=(_MY_TRACE_ALL_DATA,),
    functions_to_chart=("gc_heap::plan_phase", "gc_heap::mark_phase",),
    x_property_name="gc_index",
    y_property_names=("inclusive_count",),
    gc_filter=lambda gc: gc.Generation == Gens.Gen1,
)
```

Splitting this function call's parts:

* `ptraces_utils`: List of all the utilities objects of the traces you wish
  to chart. This parameter must be a list, so don't forget the parentheses even
  if it's just one trace.
* `functions_to_chart`: List of the functions you wish to chart.
* `x_property_name`: The chart's X-Axis metric. We want to enlist the individual
  GC's in this example, so we use the _gc\_index_ as the metric.
* `y_property_names`: List of the metrics you wish to chart.
* `gc_filter`: Function to filter the GC's you wish to chart. If you want all of
  the GC's in your trace, you can simply omit this parameter.

Running this yields the following output:

![CPU Samples Chart](images/SamplesChart.PNG)

### Show CPU Samples Metrics

The following function will show you the main CPU Samples Metrics of a given
function from your trace, in the specified time range (if any).

In this example, we will be looking at the samples metrics for the `gc_heap::plan_phase`
function, from the 1,000 msec mark to the 5,000 msec mark of the test's execution.

```python
show_cpu_samples_metrics(
    ptrace_utils=_MY_TRACE_ALL_DATA,
    function="gc_heap::plan_phase",
    start_time_msec=1000.0,
    end_time_msec=5000.0,
)
```

Splitting this function call's parts:

* `ptrace_utils`: The utilities object of the trace you wish to analyze. Note
  that as opposed to the charting function, this one only receives one trace
  instead of a list.
* `function`: Name of the function you wish to see samples metrics values.
* `start_time_msec`: Timestamp in msec where you want to begin your analysis.
  You can omit this parameter to analyze since the beginning of the trace.
* `end_time_msec`: Timestamp in msec where you want to end your analysis. You can
  omit this parameter to analyze until the end of the trace. This is the main
  reason it is vital to have the `seconds_taken` field in your _test status yaml file_.

Running this yields the following output:

![CPU Samples Metrics](images/SamplesMetrics.PNG)

## Numeric Analysis

Numeric Analysis is a feature that allows you to use the `pandas` library to
analyze overall GC metrics from various runs of a *GCPerfSim* test, or individual
GC's metrics from a given trace.

Some of the main tasks you can do with your data are, but not limited to:

* See summary statistical values (e.g. mean, min, max)
* Filter subsets of data
* Create various types of plots to graphically visualize data (scatter, histogram, line)
* Add new calculated columns for ease of access and more complicated calculations.

This feature is currently only available by means of the Jupyter Notebook and
has two subfeatures supported:

* Given a set of iterations of the same *GCPerfSim* test, read all overall
run metrics (e.g. PctTimeInGC) and build a list with them, ready for `pandas`.
* Given a single trace (this one doesn't have to be from *GCPerfSim*), read all
metrics from individual GC's, and build the list for `pandas`.

Following are the steps to set this up, as well as simple examples showing each
of the subfeatures in action.

For the full pandas documentation, you can check their [website](https://pandas.pydata.org/docs/).

### Test Run Metrics Analysis

#### Requirements

First, run any *GCPerfSim* test you want to analyze multiple times. It all depends
on your goal for how many, but when working with statistics, the more the merrier.

Once your tests are done running, open up `jupyter_notebook.py` in *VSCode* and
run the first cell for general setup. Once that is done, there is a basic
working template at the end of the notebook.

#### Setting Up Traces

```python
_BENCH = Path("bench")
_SUITE = Path("bench") / "suite"
_TRACE_PATH = _SUITE / "normal_server.yaml"

run_metrics, gc_metrics = get_test_metrics_numbers_for_jupyter(
    traces=ALL_TRACES,
    bench_file_path=_TRACE_PATH,
    run_metrics=parse_run_metrics_arg(("important",)),
    machines=None,
)

run_data_frame = pandas.DataFrame.from_dict(run_metrics).set_index("iteration_number")
gc_data_frame = pandas.DataFrame.from_dict(gc_metrics).set_index("iteration_number")
```

In this example, we ran multiple times the `normal_server` test with the `2gb` benchmark.
As shown above, the `get_test_metrics_numbers_for_jupyter()` function is in charge
of reading the traces of each time the test was run, fetching the numbers data,
processing it, and returning the dictionaries with the lists of values of each metric.

We have all the data now, but it's not ready to be consumed by `pandas`. `Pandas`
expects a dictionary which symbolizes the table you would usually build in statistics,
and transforms it into a `DataFrame` of its own. The last lines in the previous
code snippet do this. To summarize, the dictionaries are composed as follows:

**Run Metrics Dictionary**

* **Keys**: Metric Name
* **Values**: List with said metric's numbers from each iteration of the test run.

**GC Metrics Dictionary**

* **Keys**: GC Metric Name
* **Values**: List with said metric's numbers from each processed GC.

NOTE: Aside from the metric's names, the dictionaries also hold three additional keys:

* **Config_Name**: Name of the configuration run (e.g. _only\_config_).
* **Benchmark_Name**: Name of the benchmark run (e.g. _2gb_)
* **Iteration_Number**: Number of test iteration (e.g. 1)

These are used to group values by iteration, configuration and/or benchmark for more
specific analysis. There is an example at the end of the next section.

#### Perform Run Data Analysis

Now you're ready to do any statistical analysis, chart plotting, and more using
the capabilities `pandas` has to offer. The most basic example is asking for
the `describe()` method:

```python
run_data_frame.describe()
```

This shows a table with the main statistics values using the numbers you provided.

![Describe Method](images/PandasDescribe.PNG)

Another important use case to mention, is that you can also extract subsets of
data and analyze them separately. For example, here we want to visualize the
heap sizes before and after garbage collection throughout the tests we ran.

```python
heap_sizes = run_data_frame[["HeapSizeBeforeMB_Mean", "HeapSizeAfterMB_Mean"]]
heap_sizes.plot()
```

![Plot Heap Sizes](images/PandasPlot.PNG)

We can observe here that the heap sizes didn't change much between tests. You might
observe different behaviors depending on what tests you run and how the settings
are changes (e.g. a `2gb` benchmark will probably look different than a `4gb` one).

If you ran more than one configuration and/or more than one benchmark, you can
also get statistics from each one.

```python
run_data_frame.groupby(["config_name", "benchmark_name", "iteration_number"]).mean()
```

![Mean By Grouping Parameter](images/PandasMeanGroupBy.PNG)

In this example, we repeated the entire setup but this time we ran 4 different
benchmarks of a test:

* _1gb_ and _2gb_ without concurrent GC's.
* _1gb_ and _2gb_ with concurrent GC's.

We want to see the mean value for each metric from each flavor's iterations.
The `groupby()` code snippet separates the values as we require them, and then
`pandas' mean()` method calculates for each flavor, resulting in the table
shown in the picture above.

#### More Examples

The Jupyter Notebook at the root of the GC Benchmarking Infrastructure codebase
has a number of more detailed and complex examples you can follow and use for
your analysis.

### Individual GC's Metrics Analysis

#### Setting Up GC's

```python
_BENCH = Path("bench")
_SUITE = Path("bench") / "suite"
_TRACE_PATH = _SUITE / "normal_server.yaml"

_TRACE_DATA = get_trace_with_everything(_TRACE_PATH / "defgcperfsim__a__only_config__2gb__0.yaml")

gc_metrics_values = get_pergc_metrics_numbers_for_jupyter(_TRACE_DATA.gcs[0:10])

dframe = pandas.DataFrame.from_dict(gc_metrics_values)
```

In this example, we ran *GCPerfSim's* `normal_server` test with the `2gb` benchmark.
For this subfeature, you can use any trace (not necessarily from *GCPerfSim*),
but you have to write its corresponding test status `yaml` file in this case.

As shown in the code snippet above, first we read the trace by using the
function `get_trace_with_everything()`, and then the main function for getting
the data for pandas is `get_pergc_metrics_numbers_for_jupyter()`. It receives
as argument the list of GC's from the trace. To make this example simple, we
are only looking at the first 10 GC's. You can omit the subscript to analyze
all, or give the range you wish to look at.

We have all the data now, but it's not ready to be consumed by `pandas`. `Pandas`
expects a dictionary which symbolizes the table you would usually build in statistics,
and transforms it into a `DataFrame` of its own. The last line in the previous
code snippet does this. To summarize, the dictionary is composed as follows:

* **Keys**: GC Metric Name
* **Values**: List with said metric's numbers from each processed GC.

#### Perform GC Data Analysis

Once the setup in the previous section has been completed, you're ready to
do statistical analysis on GC numbers from your trace. Starting at the basic
`describe()` example, you can see overall statistics from the GC's:

```python
dframe.describe()
```

![Describe Overall GC Stats](images/PandasGCDescribe.PNG)

You will most likely want to see each GC's metrics printed nicely on a table.
This is easily done by asking `pandas` to group the data by GC Number. It needs
a statistical method to display however. Since each GC only has one value per
metric, there is no harm in calling the `mean()` method.

```python
dframe.groupby(["Number"]).mean()
```

![Show Individual GC Metrics](images/PandasGCByNumber.PNG)

Another common use case is to look only at certain metrics from the GC's as
having all of them sometimes makes it hard to find the ones you are most
interested in, at any certain point of your investigation. In this example,
we want to check the allocation rate, as well as Large Object Heap sizes.

```python
dframe[["Number", "AllocRateMBSec", "LOHSizeAfterMB", "LOHSizeBeforeMB"]].groupby("Number").mean()
```

![Show Filtered GC Metrics](images/PandasGCGroupBy.PNG)

## Programming Notes

Here are some internal implementation notes to be aware of when doing simple tests using the Jupyter Notebook.

### Individual GC Information

Each time you process a trace, each GC's information (such as whether it's Gen1,
uses compaction, etc) is stored in an object called `ProcessedGC`. Some of these
property values can show unexpected behavior in remote cases, such as not
existing, and to avoid unnecessary failures, they are wrapped in custom-defined
types called `Failable` types. The downside to this is it becomes harder to get
the actual value when doing detailed inspections of traces.

These `Failable` types are defined under `src/analysis/types.py`. They implement
the _Result_ type from the [_PyPI_ API](https://pypi.org/project/result/), which
wraps the value in either an `Ok()` or an `Err()` object.

You can either validate or extract the actual received value with the following
functions:

* To validate: Use `.is_ok()` or `.is_err()`.
* To extract the value: Use `.value`.
* If you know what behavior happened, you can also use `.ok()` and `.err()` respectively.
