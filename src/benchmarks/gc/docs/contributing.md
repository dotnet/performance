# Adding commands

To add a new command, you need to add it to the `ALL_COMMANDS` mapping in `all_commands.py`.

That file contains a sample "greet" command which shows how to create a command. The command should be a one-parameter function, and that parameter should be a `*Args` class which will be instantiated for you from command line args. (You can also have no parameters if the command takes no arguments.)

If the command outputs to the console, it's recommended to create a `Document` (from `document.py`) and then call `print_document`, instead of calling `print` directly and formatting text yourself. This makes it easier to construct tables and will format your text to the terminal's width.

# Adding a New Metric

You'll need to modify `run_metrics.md`, `single_gc_metrics.md` or `single_heap_metrics.md`.

Specifically, you'll need to modify `_RUN_METRIC_GETTERS`, `SINGLE_GC_METRIC_GETTERS` or `SINGLE_HEAP_METRIC_GETTERS`, or one of the mappings that those are generated from.

The value is some function type. Preferably, make this function an instance property of `ProcessedTrace`, `ProcessedGC`, or `ProcessHeap` and convert it to a metric getter using `fn_of_property` or `ok_of_property`. Making it an instance property makes it easier to access from other code.

Metrics always return a `Result` -- this allows the metric to fail without causing an entire command to exit with an exception. `FloatValue` is a result of a `float`. If the metric can't fail, return e.g. `float` instead of `FloatValue`, and convert the function to a `Result`-returning function with `ok_of_property`.

# Code Quality

We should make sure the code is clean with respect to the linter. Run `py . lint` to make sure it is clean. For now, do not worry about upgrading the dependencies as suggested by the linter, it won't work.

When GCPerfSim is modified, it is important to run the full default suite with both versions, the unchanged one and the modified one, to ensure no functionality was broken with the new changes and GCPerfSim works properly.

A full example on how to do this is [found here](modifying_and_testing_gcperfsim.md).

# C# and C dependencies

Non-Python code is handled by `build.py` which builds C# and C dependencies.
When you modify C# or C code (or dlls they depend on), they should automatically be rebuilt.
The code for building C dependencies is Windows-specific as currently only Windows needs these dependencies.

## TraceEvent (from PerfView)

You may need to modify TraceEvent (which is part of PerfView) when working `managed-lib`, which uses it heavily.

Run `py . use-local-trace-event path/to/perfview` to set NuGet to use the TraceEvent your local build of PerfView. Running an analysis command will cause `managed-lib` to be rebuilt using your local PerfVIew build. Use `py . undo-use-local-trace-event` to go back to using the PerfView from nuget.org.

# Using a Custom TraceEvent

You may need to modify TraceEvent (which is part of PerfView) when working `managed-lib`, which uses it heavily. To do this:

* Check out the PerfView repository, make your changes, and build.
* In `src/analysis/managed-lib/GCPerf.csproj`, you can see there are two dependencies lists, one tagged `<!-- NUGET -->` and one tagged `<!-- LOCAL -->`. You can comment out the `NUGET` one and use `LOCAL` instead. 
* Run `py . update-perfview-dlls path/to/perfview`, where `path/to/perfview` is the path to your PerfView checkout.
  This does not have any effect immediately, you'll still need to do a rebuild in a later step.
* Uncomment `#define NEW_JOIN_ANALYSIS` in `Analysis.cs` and `MoreAnalysis.cs` in `src/analysis/managed-lib`.
* Rebuild `src/analysis/managed-lib` (navigate to the directory and run `dotnet publish`).
* You'll need to set `need_join_info=True` in the call to `ALL_TRACES.get` in `jupyter_notebook.py` if you want join analysis to work there.
