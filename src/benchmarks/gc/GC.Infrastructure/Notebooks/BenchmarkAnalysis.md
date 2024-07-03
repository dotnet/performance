
# Benchmark Analysis

This notebook contains code for producing charts (and soon, tables) for GC benchmarks.  It can currently process data
from the ASP.NET benchmarks obtained using crank as well as ETL data.  One of the design points of this notebook is
that the different operations have a similar "feel"; they have many optional parameters that build on default settings.
The parameters are intended to be identical (or at least similar) across operations.

The data is organized in a hierarchy.  (See `TopLevelData`.)

- A "run" consists of multiple "configurations".  (See `RunData`.)
- A "configuration" consists of multiple "benchmarks".  (See `ConfigData`.)
- A "benchmark" consists of multiple "iterations".  (See `BenchmarkData`.)
- An "iteration" consists of multiple GCs.  (See `IterationData`.)

In addition to multiple instances of the next lower level, each level contains data appropriate for that level.
For example, an iteration of an ASP.NET benchmark will have an RPS (requests per second) score.  The overall
benchmark could have the average RPS score across the iterations (though this can also be computed at presentation-time -
more on that later).

Data is stored in a `DataManager` object.  This class has a number of `Create...` and `Add...` methods.  They process
data identically; a `Create` method is simply shorthand for `new` and `Add` and is the common usage.

`CreateAspNetData` expects the directory structure that is produced by the GC infrastructure for ASP.NET.  For example:

``` xml
<run>\<configA>_0\<benchmarkX>.<configA>_0.log
                 \<benchmarkX>.gc.etl
                 \<benchmarkY>.<configA>_0.log
                 \<benchmarkY>.gc.etl
     \<configA>_1\...
     \<configA>_2\...
     \<configA>_3\...
     \<configB>_0\...
     \<configB>_1\...
     \<configB>_2\...
     \<configB>_3\...
```

Because of the way these names are generated, do not put `.` in any name or `_` in configuration names.  The `_0`, `_1`,
etc., are the iterations.

Many operations including `CreateAspNetData` use the `Filter` class.  It is a consistent way to specify names to
include or exclude and can be done by listing names or by regular expression.  `CreateAspNetData` can filter by
config or benchmark.  (To filter by run, simply don't pass that directory to the method.)  By default, it has a list
of process names that it will look for in the ETL data, but the optional parameter `pertinentProcesses` can override
that.

`CreateGCTrace(s)` only loads ETL files.  Since there is no context for a default value, `pertinentProcesses` must be
specified.  GC traces can be loaded in two ways.  The first expects one relevant process per trace and defaults to
setting the run as blank, the config as the enclosing directory name, and the iteration as zero.  The
benchmark name is extracted from the ETL filename but can be overridden or filtered.  The second allows multiple
processes per trace.  It uses the process as the benchmark name and promotes the other values up one level (e.g.,
the ETL filename becomes the config).  This behavior is controlled by the `loadMultipleProcesses` parameter.

The data is stored in nested dictionaries that can be directly modified or accessed through a number of `Get...`
helpers.  However, typically charting (and soon tabling) methods will be called next.  There are charting methods
for each of the three levels (the "run" level is not included since aggregating across configurations is not
expected), and at each level there are two overloads that only differ based on whether they expect one metric or
a list of metrics.

- `ChartBenchmarks` will chart benchmarks across the x-axis using aggregation of data from the iterations.  Each
  run/configuration will be a data series.
- `ChartIterations` will chart benchmarks across the x-axis using data from each iteration.  Each
  run/configuration/iteration will be a data series.
- `ChartGCData` will chart GCs across the x-axis using data from each iteration.  Each run/configuration/iteration
  will be a data series, and by default each benchmark will be on a different chart.

Each charting method requires one or more metrics to include in the chart.  These are represented by the `Metric`
class, which encapsulates a way to extract the metric from the data source, a label for that data, and the unit
for that data.  Many examples of metrics are provided in the `Metrics` class.  Data from one level can be
aggregated to the next level via the `Metrics.Promote` methods and the `Aggregation` class.  For example, the
average GC pause time for the execution of a single iteration can be extracted using
`Metrics.Promote(Metrics.G.PauseDuration, Aggregation.Max)`, though this particular example is already available as
`Metrics.I.MaxPauseDuration`.  Sample GC metrics are in `Metrics.G`.  Sample iteration metrics are in `Metrics.I`.
Sample benchmark metrics are in `Metrics.B`.

For typical cases, x-axis values are handled automatically (the GC index or the benchmark name as appropriate), but
the start time of the GC can be used instead by passing `Metrics.X.StartRelativeMSec` as the optional `xMetric`
argument.  (See the class `BaseMetric` for more details on how this works.)

Each charting method accepts `Filter`s for the runs, configs, and benchmarks and a predicate `dataFilter` for the
data itself (`BenchmarkData`, `IterationData`, or `TraceGC`).

In addition, some more advanced arguments are available:

- `xArrangement` - controls how the x-axis is arranged
  - `XArrangements.Default` - normal sorting by x values
  - `XArrangements.Sorted` - each series is sorted (highest-to-lowest), and the x-axis values are changed to ranks
  - `XArrangements.CombinedSorted` - the first series is sorted (highest-to-lowest), then other series are updated
    to match the resulting ordering of x values found from that sort
  - `XArrangements.Percentile` - similar to sorted except lower-to-highest, and the x-axis values are the
    percentiles of the data within that series - `Sorted` is useful for a small number of items where the x values
    have specific meanings (such as benchmark names), whereas `Percentile` is useful when considering the x values
    as a distribution.
  - Alternatively, create a new subclass of the `XArrangement` class
- `configNameSimplifier` - XPlot has trouble if the series' names (and thus the chart legend) get too large.  The
  configuration names can be long and repetitive, so this option can be used to display shorter values.
  - `NameSimplifier.PrefixDashed` - a predefined strategy that considers configurations as a series of names
    separated by dashes.  Common prefixes are removed.  For example, `a`, `a-b-d`, `a-b-e`, and `a-c` will be
    simplified to `<>`, `b-d`, `b-e`, and `c`.  The blank value and delimiter can be adjusted by creating a new
    `PrefixSimplifier`.
  - `ListSimplifier` - applies key-value pairs to the names
  - Alternatively, create a new subclass of the `NameSimplifier` class
- `includeRunName` - By default, the run name is discarded when charting under the assumption that the typical
  case is multiple configurations under the same run.  Setting this parameter concatenates the run and configuration
  together.
- `display` - By default, generated chart(s) will be displayed.  Clearing this parameters prevents that behavior.
  Charts are always returned to the caller for possible further processing.
- `debug` - Enables a bit of debug spew.

Upcoming:

- Add the ability to specify a primary data series and add metrics that compare against it.
- Fill out the predefined metrics.
- Add requested features (specify width of chart).
- Add more aggregations, including adding the aggregation of iterations to an iteration-level chart/table.
  (e.g., b1_1, b1_2, b1_3, b1_max, b1_avg, b2_1, b2_2, b2_3, b2_max, b2_avg)
- Consider splitting `SeriesInfo` into level-specific versions and make methods such as `ChartInternal` generic
  on the series information.