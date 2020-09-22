  

# Scenario Tests Guide

  

## Overview

  

Our existing scenario tests are under `src\scenarios` in this repo, where each subdirectory contains a test asset that can be combined with a specific set of commands to do measurements. Currently we have scenario tests for [SDK](link), [Crossgen](link), [Blazor](./blazor-scenarios.md) and [other scenarios]().

  
  

## Running scenario tests

  

This is a general guideline on how the scenario tests are arranged in this repo. We will walk through it by measuring the **startup time of an empty console template** as a sample scenario. For other scenarios, refer to the following links:

  

  

-  [How to run SDK scenario tests](link)

  

-  [How to run Crossgen scenario tests](./crossgen-scenarios.md)

  

-  [How to run Blazor tests](./blazor-scenarios.md)

  

-  [How to run other Scenario tests](link)

  
### Prerequisites:
- python3 or newer
- dotnet runtime 3.0 or newer

### Step 1 Initialize Environment

  

Go to `src\scenarios` and run the following command:

  

On Windows, start a new PowerShell environment ***in Admin Mode*** and run:

  

```

cd src\scenarios

.\init.ps1

```

  

On Linux, run in the terminal:

  

```

cd src/scenarios

. init.sh

```

  
This script sets up `$PYTHONPATH`. Without other options specified it will use the dotnet in `$PATH`. To download a new dotnet or specify a custom dotnet, type `--help` for more command line options.
 
  
  

### Step 2 Run Precommand

  

For some scenarios, `pre.py` runs a defined precommand before the test run, which can but not limited to set up the asset by either creating a new template or using a static template.

  

```

cd emptyconsoletemplate

  

# format: <Python> pre.py <command> -f <target framework> -c <configuration> -r <runtime>

py -3 pre.py publish -f netcoreapp5.0 -c Release

```

  

Run `<Python> pre.py --help` for more command options.

The above command creates a new dotnet console template in `emptyconsoletemplate\app\` folder and publish it to `emptyconsoletemplate\pub\` folder.

  

### Step 3 Run Test

  

`test.py` runs the test, which defines a set of attributes for each asset. In the same directory as in the previous step, run:

```

# format: <Python> test.py <command> <test-specific options>

py -3 test.py startup

```

  

Run `<Python> test.py --help` for more command options.

The above command runs the published app under `emptyconsoletemplate\pub\` with specified iterations and measures its startup time. Traces are saved into `emptyconsoleatemplate\traces\` folder.

  
  

### Step 4 Run Postcommand

  

`post.py` should be optionally executed to clean up the artifacts.

  

```

# format: <Python> post.py

py -3 post.py

```

The above command removes `app`, `bin`, `traces`, `pub`, `tmp` directories if generated.