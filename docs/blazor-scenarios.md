
# Blazor Scenarios

## New Blazorwasm Template Size On Disk
An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](link).  The current document has specific instruction to run blazor scenario tests. 
### Step 1 Initialize Environment
Same instruction of [Step 1 in Scenario Tests Guide](scenarios-workflow.md).
### Step 2 Run Precommand
```
cd blazor
py -3 pre.py publish --msbuild "/p:_TrimmerDumpDependencies=true"
```
The above command creates a new blazorwasm template in `blazor\app\` and publish it to `blazor\pub\`. The `--msbuild "/p:_TrimmerDumpDependencies=true"` argument is optional and can be specified to generate linker dump from the build, which is saved to `blazor\traces\`. 

### Step 3 Run Test
```
py -3 test.py sod --scenario-name "SOD - New Blazor Template - Publish"
```
This runs the test. Note `--scenario-name` is optional.
### Step 4 Run Postcommand
Same instruction of [Step 4 of Scenario Tests Guide](scenarios-workflow.md).

