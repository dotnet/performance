
# Crossgen Scenarios

An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](./scenarios-workflow.md). The current document has specific instruction to run:

- [Crossgen Throughput Scenario](#crossgen-throughput-scenario)
- [Crossgen2 Throughput Scenario](#crossgen2-throughput-scenario)
- [Size on Disk Scenario](#size-on-disk-scenario)

## Before Running Any Scenario

### Prerequisites

- python3 or newer
- dotnet runtime 3.0 or newer
- terminal/command prompt **in Admin Mode** (for collecting kernel traces)
- clean state of the test machine (anti-virus scan is off and no other user program's running -- to minimize the influence of environment on the test)

### 1. Generate Core_Root

These performance tests use the built runtime test directory [Core_Root](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/using-corerun.md) for the crossgen tool itself and other runtime assmblies as compilation input. Core_Root is an intermediate output from the runtime build, which contains runtime assemblies and tools.

You can skip this step if you already have Core_Root. To generate Core_Root directory, first clone [dotnet/runtime repo](https://github.com/dotnet/runtime) and run:

```cmd
src\tests\build.cmd Release <arch> generatelayoutonly
```

[the instruction of building coreclr tests](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/coreclr/windows-test-instructions.md), which creates Core_Root directory.

If the build's successful, you should have Core_Root with the path like:

```cmd
 runtime\artifacts\tests\coreclr\<OS>.<Arch>.<BuildType>\Tests\Core_Root
```

### 2. Initialize Environment

Same instruction of [Scenario Tests Guide - Step 1](./scenarios-workflow.md#step-1-initialize-environment).

## Crossgen Throughput Scenario

**Crossgen Throughput** is a scenario test that measures the throughput of [crossgen compilation](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/crossgen.md). To be more specific, our test *implicitly* calls

```cmd
.\crossgen.exe <assembly to compile>
```

with other applicable arguments and measures its throughput. We will walk through crossgen compiling `System.Private.Xml.dll` as an example.

Ensure you've first followed the [preparatory steps](#before-running-any-scenario).

### 1. Run Precommand

For **Crossgen Throughput** scenario, unlike other scenarios there's no need to run any precommand (`pre.py`). Just switch to the test asset directory:

```terminal
cd crossgen
```

### 2. Run Test

Now run the test, in our example we use `System.Private.Xml.dll` under Core_Root as the input assembly to compile, and you can replace it with other assemblies **under Core_Root**.

```cmd
python3 test.py crossgen --core-root <path to core_root>\Core_Root --single System.Private.Xml.dll
```

This will run the test harness [Startup Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/Startup), which runs crossgen compilation in several iterations and measures its throughput. The result will be something like this:

```cmd
[2020/09/25 09:54:48][INFO] Parsing traces\Crossgen Throughput - System.Private.Xml.etl
[2020/09/25 09:54:49][INFO] Crossgen Throughput - System.Private.Xml.dll
[2020/09/25 09:54:49][INFO] Metric         |Average        |Min            |Max
[2020/09/25 09:54:49][INFO] ---------------|---------------|---------------|---------------
[2020/09/25 09:54:49][INFO] Process Time   |7295.825 ms    |7295.825 ms    |7295.825 ms
[2020/09/25 09:54:49][INFO] Time on Thread |7276.019 ms    |7276.019 ms    |7276.019 ms
```

### 3. Run Postcommand

Same instructions as [Scenario Tests Guide - Step 4](./scenarios-workflow.md#step-4-run-postcommand).

## Crossgen2 Throughput Scenario

Compared to `Crossgen Throughput` scenario, `Crossgen2 Throughput` Scenario measures more metrics, which are:

- Process Time (Throughput)
- Loading Interval
- Emitting Interval
- Jit Interval
- Compilation Interval

Steps to run **Crossgen2 Throughput** scenario are very similar to those of **Crossgen Throughput**. In addition to compilation of a single file, composite compilation is enabled in crossgen2, so the test command is different.

Ensure you've first followed the [preparatory steps](#before-running-any-scenario).

### 1. Run Precommand

Same as **Crossgen Throughput** scenario, there's no need to run any precommand (`pre.py`). Just switch to the test asset directory:

```cmd
cd crossgen2
```

### 2. Run Test

For scenario which compiles a **single assembly**, we use `System.Private.Xml.dll` as an example, you can replace it with other assembly **under Core_Root**:

```cmd
python3 test.py crossgen2 --core-root <path to core_root>\Core_Root --single System.Private.Xml.dll
```

For scenario which does **composite compilation**, we try to compile the majority of runtime assemblies represented by [framework-r2r.dll.rsp](https://github.com/dotnet/performance/blob/main/src/scenarios/crossgen2/framework-r2r.dll.rsp):

```cmd
python3 test.py crossgen2 --core-root <path to core_root>\Core_Root --composite <repo root>/src/scenarios/crossgen2/framework-r2r.dll.rsp
```

Note that for the composite scenario, the command line can exceed the maximum length if it takes a list of paths to assemblies, so an `.rsp` file is used to avoid it.  `--composite <rsp file>` option refers to a rsp file that contains a list of assemblies to compile. A sample file [framework-r2r.dll.rsp](https://github.com/dotnet/performance/blob/main/src/scenarios/crossgen2/framework-r2r.dll.rsp) can be found under `crossgen2\` folder.

The test command runs the test harness [Startup Tool](https://github.com/dotnet/performance/tree/main/src/tools/ScenarioMeasurement/Startup), which runs crossgen2 compilation in several iterations and measures its throughput. The result should partially look like:

 ```cmd
 [2020/09/25 10:25:09][INFO] Merging traces\Crossgen2 Throughput - Single - System.Private.perflabkernel.etl,traces\Crossgen2 Throughput - Single - System.Private.perflabuser.etl...
[2020/09/25 10:25:11][INFO] Trace Saved to traces\Crossgen2 Throughput - Single - System.Private.etl
[2020/09/25 10:25:11][INFO] Parsing traces\Crossgen2 Throughput - Single - System.Private.etl
[2020/09/25 10:25:15][INFO] Crossgen2 Throughput - Single - System.Private.CoreLib
[2020/09/25 10:25:15][INFO] Metric               |Average        |Min            |Max
[2020/09/25 10:25:15][INFO] ---------------------|---------------|---------------|---------------
[2020/09/25 10:25:15][INFO] Process Time         |13550.728 ms   |13550.728 ms   |13550.728 ms
[2020/09/25 10:25:15][INFO] Loading Interval     |1090.205 ms    |1090.205 ms    |1090.205 ms
[2020/09/25 10:25:15][INFO] Emitting Interval    |1330.489 ms    |1330.489 ms    |1330.489 ms
[2020/09/25 10:25:15][INFO] Jit Interval         |9464.402 ms    |9464.402 ms    |9464.402 ms
[2020/09/25 10:25:15][INFO] Compilation Interval |12827.350 ms   |12827.350 ms   |12827.350 ms
 ```

### 3. Run Postcommand

```cmd
python3 post.py
```

## Size on Disk Scenario

The size on disk scenario for crossgen/crossgen2 measures the sizes of generated ready-to-run images. These tests use the precommand to generate a ready-to-run image, then run the size-on-disk tool on the containing directory.

Ensure you've first followed the [preparatory steps](#before-running-any-scenario).

### 1. Run Precommand

```cmd
cd crossgen|crossgen2
python3 pre.py crossgen|crossgen2 --core-root <path to core_root> --single System.Private.Xml.dll
```

`--single` takes any framework assembly available in core_root.

### 2. Run Test

For scenario which compiles a **single assembly**, we use `System.Private.Xml.dll` as an example, you can replace it with other assembly **under Core_Root**:

```cmd
python3 test.py sod --dirs ./crossgen.out
```

The size-on-disk tool outputs an accounting of the file sizes under the crossgen output directory:

```cmd
[2020/10/26 19:00:06][INFO] Crossgen2 Size On Disk
[2020/10/26 19:00:06][INFO] Metric                                   |Average           |Min               |Max
[2020/10/26 19:00:06][INFO] -----------------------------------------|------------------|------------------|------------------
[2020/10/26 19:00:06][INFO] Crossgen2 Size On Disk                   |8412672.000 bytes |8412672.000 bytes |8412672.000 bytes
[2020/10/26 19:00:06][INFO] Crossgen2 Size On Disk - Count           |1.000 count       |1.000 count       |1.000 count
[2020/10/26 19:00:06][INFO] .\crossgen\                              |8412672.000 bytes |8412672.000 bytes |8412672.000 bytes
[2020/10/26 19:00:06][INFO] .\crossgen\ - Count                      |1.000 count       |1.000 count       |1.000 count
[2020/10/26 19:00:06][INFO] .\crossgen\System.Private.Xml.ni.dll     |8412672.000 bytes |8412672.000 bytes |8412672.000 bytes
[2020/10/26 19:00:06][INFO] Aggregate - .dll                         |8412672.000 bytes |8412672.000 bytes |8412672.000 bytes
[2020/10/26 19:00:06][INFO] Aggregate - .dll - Count                 |1.000 count       |1.000 count       |1.000 count
```

### 3. Run Postcommand

```cmd
python3 post.py
```

## Command Matrix

For the purpose of quick reference, the commands can be summarized into the following matrix:

| Scenario                               | Asset Directory | Precommand                                                                     | Testcommand                                                                       | Postcommand | Supported Framework | Supported Platform      |
|----------------------------------------|-----------------|--------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|-------------|---------------------|-------------------------|
| Crossgen Throughput                    | crossgen        | N/A                                                                            | test.py crossgen --core-root \<path to Core_Root> --single \<assembly name>       | post.py     | N/A                 | Windows-x64;Windows-x86 |
| Crossgen2 Throughput (single assembly) | crossgen2       | N/A                                                                            | test.py crossgen2 --core-root \<path to Core_Root> --single \<assembly name>      | post.py     | N/A                 | Windows-x64;Linux       |
| Crossgen2 Throughput (composite)       | crossgen2       | N/A                                                                            | test.py crossgen2 --core-root \<path to Core_Root> --composite \<path to .rsp>    | post.py     | N/A                 | Windows-x64;Linux       |
| Crossgen Size on Disk                  | crossgen        | pre.py crossgen --core-root \<path to Core_Root> --single \<assembly name>     | test.py sod --dirs crossgen.out                                                   | post.py     | N/A                 | Windows-x64;Linux       |
| Crossgen2 Size on Disk                 | crossgen2       | pre.py crossgen2 --core-root \<path to Core_Root> --single \<assembly name>    | test.py sod --dirs crossgen.out                                                   | post.py     | N/A                 | Windows-x64;Linux       |

## Relevant Links

[Crossgen2 Compilation Structure Enhancements](https://github.com/dotnet/runtime/blob/main/docs/design/features/crossgen2-compilation-structure-enhancements.md)
