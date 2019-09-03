# Profiling workflow for CoreFX

## Table of Contents

- [Prerequisites](#Prerequisites)
  - [Build](#Build)
  - [Repro](#Repro)
  - [Project Settings](#Project-Settings)
- [Profiling on Windows](#Profiling-on-Windows)
  - [Visual Studio Profiler](#Visual-Studio-Profiler)
    - [dotnet](#dotnet)
    - [CPU](#CPU)
    - [CoreRun](#CoreRun)
    - [Allocation Tracking](#Allocation-Tracking)
  - [PerfView](#PerfView)
    - [CPU Investigation](#CPU-Investigation)
      - [Filtering](#Filtering)
      - [Analyzing the Results](#Analyzing-the-Results)
      - [Viewing Source Code](#Viewing-Source-Code)
      - [Identifying Regressions](#Identifying-Regressions)


## Prerequisites

This doc explains how to profile local CoreFX and CoreCLR builds and it's targetted at CoreFX and CoreCLR contributors.

### Build

If you are interested in profiling some parts of `System.Private.CoreLib.dll` and **being able to see the source code in Visual Studio Profiler**, you need to build CoreCLR in Release first:

```cmd
C:\Projects\coreclr> build -release
```

The next step is to build CoreFX in Release and provide the path to CoreCLR via [CoreCLROverridePath](https://github.com/dotnet/corefx/blob/0e7236fda21a07302b14030c82f79bb981c723a6/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits) build parameter:

```cmd
C:\Projects\corefx> build -c Release /p:CoreCLROverridePath="C:\Projects\coreclr\bin\Product\Windows_NT.x64.Release"
```

If you don't care about detailed `System.Private.CoreLib.dll` info you can just build the CoreFX itself:

```cmd
C:\Projects\corefx> build -c Release
```

The build produces two things that we care about:

* `dotnet` and all `System.XYZ.dlls` used internally by CoreFX to run unit tests. It can be used by Visual Studio Profiler to run the code that you want to profile. Example:

```log
C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\dotnet.exe
```

* `CoreRun` and all `System.XYZ.dlls` that can be used to run the code that you want to profile. Example:

```log
C:\Projects\corefx\artifacts\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe
```

Once you rebuild the part of CoreFX you are working on, the appropriate `.dll` gets updated and the next time you run profiler, dotnet|CoreRun is going to use the updated library.

```cmd
C:\Projects\corefx\src\System.Text.RegularExpressions\src> dotnet msbuild /p:ConfigurationGroup=Release
```

### Repro

The next step is to prepare a small console app that executes the code that you want to profile. The app **should run for at least a few seconds** and **keep the overhead as small as possible to make sure it does not dominate the profile**.

Following code can be used to profile the [DateTime.UtcNow](https://github.com/dotnet/coreclr/issues/25728):

```cs
using System;

namespace ProfilingDocs
{
    class Program
    {
        static int Main()
        {
            long ticks = 0;

            for (int i = 0; i < 30_000_000; i++) // 30_000_000 is enough to run for 2-3 seconds
            {
                ticks ^= DateTime.UtcNow.Ticks; // xor is cheap operation (modulo is not)
            }

            return (int)ticks; // return the result to prevent from dead code elimination
        }
    }
}
```

**Hint**: If you need to implement some initialization logic you can extract it to a standalone method and use `[MethodImpl]` to prevent from inlining. It's going to make it easier for you to filter the profiler output: instead of filtering by time you are going to be able to filter to a single method.

```cs
class Program
{
    static void Main() => WhatYouCareAbout(Setup());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static $SomeType Setup() { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WhatYouCareAbout($SomeType initialized) { }
}
```

### Project Settings

It's recommended to disable Tiered JIT (to avoid the need of warmup) and emit full symbols (not enabled by default for Release builds):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.0</TargetFramework>

    <DebugType>pdbonly</DebugType>
    <DebugSymbols>true</DebugSymbols>

    <TieredCompilation>false</TieredCompilation>
  </PropertyGroup>

</Project>

```

## Profiling on Windows

### Visual Studio Profiler

Visual Studio Profiler is not as powerful as PerfView, but it's definitely more intuitive to use. If you don't know which profiler to use, you should use it by default.

To profile a local build of CoreFX and get symbol solving working in Visual Studio Profiler you can use the produced `dotnet` or `CoreRun`.

#### dotnet

Following script launches a Visual Studio solution with environment variables required to use a local version of the .NET Core SDK:

```cmd
@ECHO OFF
SETLOCAL

:: This tells .NET Core to use the dotnet.exe passed as first argument to the script
SET DOTNET_ROOT=%1

:: This tells .NET Core not to go looking for .NET Core in other places
SET DOTNET_MULTILEVEL_LOOKUP=0

:: Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
SET PATH=%DOTNET_ROOT%;%PATH%

SET sln=%2

:: This starts VS
start %sln%
```

You can just save it as `startvs.cmd` file and run providing path to the `testhost` folder produced by CoreFX build and a VS solution with repo project:

```cmd
startvs.cmd "C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\" "C:\Projects\repro\ProfilingDocs.sln"
```

#### CPU

Once you started the VS with the right environment variables you need to click on the `Debug` menu item and then choose `Performance Profiler` or just press `Alt+F2`:

![Debug Performance Profiler](img/vs_profiler_0_debug_performance_profiler.png)

Switch to `Release` configuration, check `CPU Usage` and just click `Start`:

![Configuration](img/vs_profiler_1_configuration.png)

Give VS a few seconds to run. It's important to execute enough code to run for at least a few seconds to let the profiler gather enough samples:

![Few seconds](img/vs_profiler_2_few_seconds.png)

By default VS Profiler does not show the [External Code](https://docs.microsoft.com/en-us/visualstudio/profiling/beginners-guide-to-performance-profiling?view=vs-2019#view-external-code) (functions in system and framework component). When you are profiling CoreFX or CoreCLR this is **the most important thing you care about**. To show the external code click `Filter` then check `Show External Code` and click `Apply` button.

![Show External code](img/vs_profiler_3_external_code.png)

If you don't do that, then VS is going to tell you that most of the time was spent in "External Code" but you won't be able to tell what code was it exactly.

![External code](img/vs_profiler_4_external_code.png)

From here, you should be able to [analyze](https://docs.microsoft.com/en-us/visualstudio/profiling/beginners-guide-to-performance-profiling?view=vs-2019#step-2-analyze-cpu-usage-data) the profile.

You can either click on the `Main` method and drill into interesting methods:

![Go to caller callee](img/vs_profiler_5_go_to_caller_callee.png)

Or just sort the methods by exclusive CPU time by clicking on the `Self CPU` column and clicking on the most time-consuming method name.

![Most time consuming](img/vs_profiler_6_sort_by_self_cpu_time.png)

If you have configured everything properly you are able to see the CPU time spent per source code line!

![External code](img/vs_profiler_7_source_code.png)

#### CoreRun

If you prefer to use CoreRun instead of dotnet you need to select `Launch an executable`

![Launch executable](img/vs_profiler_8_executable_corerun.png)

And provide the neccessary arguments (path to the compiled repro dll):

![Executable parameters](img/vs_profiler_9_corerun_run.png)

The alternative is to run the repro app using CoreRun yourself and use VS Profiler to attach to a running process:

![Attach to running process](img/vs_profiler_10_corerun_attach.png)

![Choose CoreRun process](img/vs_profiler_11_corerun_attach.png)


#### Allocation Tracking

Since `DateTime.UtcNow` does not allocate managed memory, we are going to profile a different app:

```cs
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace ProfilingDocs
{
    class Program
    {
        static void Main()
        {
            for (int i = 0; i < 100_000; i++)
            {
                Consume(CryptoConfig.CreateFromName("RSA"));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume<T>(in T _) { }
    }
}
```

To track memory allocation you need to check `.NET Object Allocation Tracking` in the performance wizard and click `Start`.

![.NET Object Allocation Tracking](img/vs_profiler_12_check_memory.png)

The reported results contain information about all allocated .NET objects.

![Allocated Objects](img/vs_profiler_13_memory.png)

Again, if you have configured everything properly you are able to right click on method name and Go to Source File!

![Go to Source File](img/vs_profiler_14_memory_goto_source.png)

![Actual Source File](img/vs_profiler_15_memory_source_file.png)

### PerfView

PerfView is the ultimate .NET Profiler and if you are new to PerfView it's recommended to read it's tutorial or watch the tutorial [videos](https://channel9.msdn.com/Series/PerfView-Tutorial).

![Welcome Screen](img/perfview_0_welcome.png)

#### CPU Investigation

We can **Collect** profile data by either **Run**ning a standalone executable (or command) or **Collect**ing the data machine wide with explicit start and stop.

![Collect Menu Items](img/perfview_1_collect.png)

When profiling local CoreFX build it's recommended to Run a standalone repro app using CoreRun.

![Run CoreRun](img/perfview_2_run_corerun.png)

If for some reason PerfView fails to run the command you need to click the `Log` button in left bottom corner and read the logs to find out what happened.

![Log button](img/perfview_3_log_button.png)

Once PerfView finishes the run you need to open the `CPU Stacks`

![CPU Stacks](img/perfview_4_cpu_stacks.png)

By default PerfView collects the profile data machine wide. Once you click on the `CPU Stacks` it shows you a window with the list of processes active during profile session. It sorts the processes descending by CPU time so the most active processes are at the top. You just need to select `CoreRun` and click `OK`.

![Select Process](img/perfview_5_select_process.png)

Once you do that it opens the `CPU Stacks` window with the default filtering applied.

![Non filtered CPU Stacks](img/perfview_6_cpu_stacks_not_filtered.png)

The first thing you need to do is to make sure that you have at least a few 1000 samples (preferably over 5000) and that the process is actually CPU bound.

The `Metric/Interval` is a quick measurement of how CPU bound the trace is as a whole. A value of 1 indicates a program that on average consumes all the CPU from a single processor. Unless that is high, your problem is not CPU (it can be some blocking operation like network/disk read).

![CPU Metric](img/perfview_7_cpu_metric.png)

##### Filtering

Fundamentally, what is collected by the PerfView profiler is a sequence of stacks. A stack is collected every millisecond for each hardware processor on the machine. This is very detailed information and hence by default PerfView groups the stacks. This is very useful when you are profiling a real-world application in a production environment, but when you work on the .NET Team and you profile some simple repro app you care about all details and you don't want the results to be grouped by modules.

![Group pats](img/perfview_8_grouppats_on.png)

This is why you should disable the grouping by selecting `[no grouping]` item from `GroupPats` combo box. If you want to learn more about `GroupPats` just click on it (it looks like a web link and it opens help).

![No grouping](img/perfview_9_grouppats_off.png)

Now you can see much more details:

![No grouping applied](img/perfview_10_no_grouping_applied.png)

But some of the results contain `!?` which means unsolved symbols.

![Unsolved symbols](img/perfview_11_unsolved_symbols.png)

To solve the symbols you just need to select given names, right click and choose `Lookup symbols`. You can also just press `Ctrl+A` (select all) and `Alt+S` (Lookup symbols).

![Lookup symbols](img/perfview_12_lookup_symbols.png)

PerfView does an excellent job at solving symbols. It's capable of solving not only the app code symbols but also .NET Runtime and Windows OS symbols. It's one of the moments when it shines compared to other profilers that very often struggle with that.

When we are using the `Collect -> Run` option PerfView profiles entire process lifetime. It's very useful when you are interested in things like .NET Runtime startup performance. But typically you just care about specific method. So the next thing you need to do is to set the Time Range. You can do that by selecting the interesting method, right clicking and choosing `Set Time Range`. Or just pressing `Alt+R`.

![Set Time Range](img/perfview_13_set_time_range.png)

As you can see, all the methods that were executed before the first and after last call to `DateTime.UtcNow` got filtered out:

![Time Range Applied](img/perfview_14_set_time_range_before_vs_after.png)

`When` - This is a visualization of how the INCLUSIVE samples collected for that node vary over time.   The total range (from the Start and End text boxes), is divided into 32 time 'TimeBuckets' and the inclusive samples for that node are accumulated into those 32 buckets.   Each bucket is then represented as a digit that represents a scaled value.

This simple text representation of histogram can be very useful when profiling more complex scenarios, but in this case it just shows us that `DateTime.UtcNow` was executed all the time. But this is exactly what we wanted!

##### Analyzing the Results

Once we get the data filtered we can start the analysis.

The `By Name` tab contains the list of all methods captured during profile time. This list is by default sorted in descending order by **Exc**lusive CPU time. It means that the most expensive methods which perform actual CPU computation are at the top of the table. These are the methods you care about.

![By name tab](img/perfview_15_by_name.png)

To find out who is calling the most time consuming method you can right click on it and select `Goto -> Goto Item in Callers` or just press `F10`.

![Go to callers](img/perfview_16_go_to_callers.png)

![Callers](img/perfview_17_callers.png)

If you wish you can  see the entire `Call Tree` by clicking on the `Call Tree` tab:

![Call Tree](img/perfview_18_call_tree.png)

`Flame Graph` offers you a different form of visualization::

![Flame Graph](img/perfview_19_flame_graph.png)

The graph starts at the bottom. Each box represents a method in the stack (inclusive CPU time). Every parent is the caller, children are the callees. The wider the box, the more time it was on-CPU. 

For the leaf nodes the inclusive time == exclusive time. The difference between the parent and children box width (marked with red on the image below) is the exclusive parent (caller) time.

```cs
parent.InclusiveTime - children.InclusiveTime = parent.ExclusiveTime
```

![Flame Graph Exclusive time](img/perfview_20_flame_graph_exclusive_time.png)

##### Viewing Source Code

If you want to view the Source Code of the given method you need to right-click on it and select `Goto Source (Def)` menu item. Or just press `Alt+D`.

![Goto Source](img/perfview_21_goto_source.png)

![Source Code](img/perfview_22_source_code.png)

If PerfView fails to show you the source code you should read the `Log` output. If you are unable to make it work and you really care about it you should switch to Visual Studio Profiler.

**Note:** As of today, PerfView keeps the `.pdb` files [opened](https://github.com/microsoft/perfview/pull/979) after showing the source code. It means that if you keep the trace file opened in PerfView and try to rebuild CoreFX/CLR the build is going to fail. You might need to close PerfView to rebuild CoreFX/CLR.

##### Identifying Regressions

PerfView has a built-in support for identifying regressions. To use it you need to:

* profile two different versions of .NET Core running exact same C# code
* open both trace files in PerfView and apply exactly the same filters
* choose `Regression -> With Baseline` from the top Menu

![Two trace files](img/perfview_23_regression.png)

![Regression Report](img/perfview_24_regression_report.png)

It's recommended to use it instead of trying to eyeball complex Flame Graphs.
