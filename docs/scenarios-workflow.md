  

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
- terminal/command prompt **in Admin Mode** (for collecting kernel traces)
- clean state of the test machine (anti-virus scan is off and no other user program's running -- to minimize the influence of environment on the test)

### Step 1 Initialize Environment
Before running the test, it is important to choose the right version of dotnet to test. Follow the guidance below to set up `PYTHONPATH` (to run our Python test harness) and dotnet directory for the desired test environment. This step is applicable to all scenarios and can only be run once for one environment.

Go to `src\scenarios` and run the following command:
#### Windows
Start a new PowerShell environment ***in Admin Mode*** and run:

```
cd src\scenarios
.\init.ps1
```
The next steps will need to run in the same Powershell environment. You can also specify custom dotnet directory or download a new dotnet to use. Add `-Help` option for more information.

#### Linux
Start a new bash terminal ***with Root Access*** and run:
```
cd src/scenarios
. ./init.sh
```
The next steps will need to run in the same bash environment. You can also specify custom dotnet directory or download a new dotnet to use. Add `-h` or `-help` option for more information.


### Step 2 Run Precommand
Now you have `PYTHONPATH` set and dotnet to test in `PATH`, the next step is to run precommand to set up the specific test asset. Precommand is necessary for some scenarios and different test assets require different commands. **NOTE: for each test asset, not all commands are supported. Please refer to [Command Matrix](#command-matrix) for available scenarios.**

For some scenarios (not all), `pre.py` runs a defined precommand before the test run, which can but not limited to set up the asset by either creating a new template or using a static template.

Format for running precommands:
```
cd <asset directory> # switch to the specific asset directory
python3 pre.py <command> <options>  # run precommand
```
In our **startup time of an empty console template** example, we can run
```
cd emptyconsoletemplate
python3 pre.py publish -f netcoreapp5.0 -c Release
```
The above command creates a new dotnet console template in `emptyconsoletemplate\app\` folder, builds the project targeting netcoreapp5.0 in Release and publishs it to `emptyconsoletemplate\pub\` folder.

Run `python3 pre.py --help` for more command options and their meanings.


### Step 3 Run Test
Upon this step, the project source code should exist under `app\` directory. There should be published output under `pub\` if the precommand is "publish", and built output under `bin\` if the precommand is "build". Now the test should be ready to run. **NOTE: for each test asset, not all commands are supported. Please refer to [Command Matrix](#command-matrix) for available scenarios.**

`test.py` runs the test with a set of defined attributes. 
Format for running test commands:
```
python3 test.py <command> <test-specific options>
```
In our **startup time of an empty console template example**, we can run
```
python3 test.py startup
```
The above command runs the published app under `pub\` with specified iterations and measures its startup time. 

Test report and traces are saved into `emptyconsoleatemplate\traces\` directory.

Run `python3 test.py --help` for more command options and their meanings.

### Step 4 Run Postcommand

`post.py` should be optionally executed to clean up the artifacts. It's the same command for all scenarios.

```
py -3 post.py
```
The above command removes `app`, `bin`, `traces`, `pub`, `tmp` directories if generated.

### Command Matrix
Some command options are only applicable for certain test assets. Refer to the command matrix for each scenario category for a list of available command combinations:
- [SDK Command Matrix](.\sdk-scenarios.md#command-matrix)
- [Crossgen Command Matrix](.\crossgen-scenarios.md#command-matrix)
- [Blazor Command Matrix](.\blazor-scenarios.md#command-matrix)
- [Other Scenarios Command Matrix](.\other-scenarios.md#command-matrix)