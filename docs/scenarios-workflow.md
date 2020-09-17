# Scenario Tests Guide
## Overview
Our existing scenario tests are under `src\scenarios` in this repo, where each subdirectory contains a test asset that can be combined with a specific set of commands to do measurements. Currently we have scenario tests for [SDK](link), [Crossgen](link), [Blazor](link) and [other scenarios](). 
<br>
<br>
<br>
## Running scenario tests
This is a general guideline on how the scenario tests are arranged in this repo. For specific commands, refer to the following links:

- [How to run SDK scenario tests](link)
- [How to run Crossgen scenario tests](link)
- [How to run Blazor tests](link)
- [How to run other Scenario tests](link)
<br>
<br>
### Step 1 Initialize Environment
Go to `src\scenarios` and run the following command:
On Windows, start a new PowerShell environment and run:

```
cd src\scenarios
.\init.ps1
```

On Linux, run in the terminal:

```
cd src/scenarios
. init.sh 
```

Command options for `init.ps1` and `init.sh` scripts:
```
.\init.ps1 -DotnetDirectory <custom dotnet directory> # specify a custom dotnet location
.\init.ps1 -Channel <Channel to download new dotnet> # channel to download a new dotnet from, which will be downloaded into tools folder
```

Same mearning for `init.sh` on Linux but with `-dotnetdir` and `-channel` options
<br>
<br>
### Step 2 Run Precommand
`pre.py` runs a defined precommand before the test run, which can but not limited to set up the asset by either creating a new template or using a static template.
```
cd <one of the asset folders under src\scenarios>
<Python> pre.py <command> -f <target framework> -c <configuration> -r <runtime>
```
Run `<Python> pre.py --help` for more command options.
<br>
<br>
### Step 3 Run Test
`test.py` runs the test, which defines a set of attributes for each asset. In the same directory as in the previous step, run:

```
<Python> test.py <command> <test-specific options>
```

Run `<Python> test.py --help` for more command options.
<br>
<br>
### Step 4 Run Postcommand
`post.py` should be optionally executed to clean up any artifacts.
```
<Python> post.py
```

