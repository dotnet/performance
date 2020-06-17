# Using with jupyter notebook

A jupyter notebook has already been set up in `jupyter_notebook.py`. So far it's only been tested with VSCode.

## Using with VSCode

* Run `code .` in the `/performance/src/benchmarks/gc` directory.
* Open `jupyter_notebook.py`.
* Open your settings and enable `"editor.codeLens": true,`.
* Wait a minute for CodeLens to show up in the notebook.

## Overview

* Click on `Run cell` in the top cell. This is the only cell that is not optional to run.
* Each of the other cells corresponds to some command.
  Instead of providing command line arguments, edit the code to provide different arguments to the function.
  You can then re-run that cell without needing to reload traces.
* The top cell specifies all the trace files and metrics that will be available.
  If you need to add new files or metrics you will need to rerun the top cell.
* If you edit code in any other file you will have to reload the whole notebook.
  See the ⟲ icon in the top-right.
* You can also do any custom analysis on the trace. The `custom` section at the bottom shows how to manually sum all GC times.

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
