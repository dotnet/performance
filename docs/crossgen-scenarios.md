
# Crossgen Scenarios
An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](./scenarios-workflow.md).  The current document has specific instruction to run:

- [Crossgen Throughput](#crossgen-throughput)
- [Crossgen2 Throughput](#crossgen2-throughput)


## Crossgen Throughput

### Step 0 Generate Core Root
Build CoreCLR in dotnet/runtime repo, which creates Core Root. Instruction can be found [here](https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/README.md). A detailed introduction of Core Root can be found [here](https://github.com/dotnet/runtime/blob/master/docs/workflow/testing/using-corerun.md).
### Step 1 Initialize Environment
Same instruction of [Scenario Tests Guide - Step 1](./scenarios-workflow#step-1-initialize-environment).
### Step 2 Run Precommand
```
cd crossgen
# No precommand needed for this scenario
```
### Step 3 Run Test
```
py -3 test.py crossgen --core-root <path to Core Root directory> --test-name <assembly to compile>
```
The above command runs the test. Note `--test-name <assembly to compile>` option refers to the relative path of an assembly that's under Core Root directory. For example, the option can be `--test-name System.Private.Xml.dll` so the test measures the throughput of crossgen compiling `System.Private.Xml.dll`.
### Step 4 Run Postcommand
Same instruction of [Scenario Tests Guide - Step 4](./scenarios-workflow#step-4-run-postcommand).

## Crossgen2 Throughput
Refer to [Crossgen Throughput](crossgen-throughput) for Step 0,1 and 4
### Step 2 Run Precommand
```
cd crossgen2
# No precommand needed for this scenario
```
### Step 3 Run Test
For scenario which compiles a **single assembly**:
```
py -3 test.py --core-root <path to Core Root directory> --single <assembly to compile>
```
Note `--test-name <assembly to compile>` option refers to the relative path of an assembly that's under Core Root directory. For example, the option can be `--test-name System.Private.Xml.dll` so the test measures the throughput of crossgen compiling `System.Private.Xml.dll`.
For scenario which does **composite compilation**:
```
py -3 test.py --core-root <path to Core Root directory> --composite <rsp file>
```
Note that for the composite scenario, the command line can exceed the maximum length if it takes a list of paths to assemblies, so an `.rsp` file should be used.  `--composite <rsp file>` option refers to a rsp file that contains a list of assemblies to compile. A sample file [framework-r2r.dll.rsp](https://github.com/dotnet/performance/blob/master/src/scenarios/crossgen2/framework-r2r.dll.rsp) can be found under `crossgen2\` folder.
 
### Relevant Links
- [How to use Crossgen](https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/crossgen.md)
- [How to build runtime](https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/README.md)
- [What is Core Root](https://github.com/dotnet/runtime/blob/master/docs/workflow/testing/using-corerun.md)