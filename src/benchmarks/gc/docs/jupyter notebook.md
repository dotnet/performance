# Using with jupyter notebook

A jupyter notebook has already been set up in `jupyter_notebook.py`. So far it's only been tested with VSCode.


## Using with VSCode

* Run `code .` in the `dotnet-gc-infra` directory.
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
