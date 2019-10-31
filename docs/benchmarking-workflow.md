# .NET benchmarking workflow

## Table of Contents

- [Pre-requisites](#Pre-requisites)
  - [Clone](#Clone)
  - [Build](#Build)
- [Choosing the benchmarks](#Choosing-the-benchmarks)
  - [Filtering Examples](#Filtering-Examples)
  - [Benchmark not found](#Benchmark-not-found)
- [Running the benchmarks](#Running-the-benchmarks)
  - [Against single runtime](#Against-single-runtime)
  - [Against multiple runtimes](#Against-multiple-runtimes)
  - [Against single runtime using given dotnet cli](#Against-single-runtime-using-given-dotnet-cli)
  - [Against private runtime build](#Against-private-runtime-build)
- [Workflows](#Workflows)
  - [Improving the performance](#Improving-the-performance)
  - [Checking for regressions](#Checking-for-regressions)
  - [Comparing different runtimes](#Comparing-different-runtimes)
  - [New API](#New-API)

## Pre-requisites

### Clone

To run the benchmarks you need to clone the `dotnet/performance` repository first.

```cmd
git clone https://github.com/dotnet/performance.git
```

This repository is **independent from CoreFX and CoreCLR build systems!** So you need to do this **only ONCE**, no matter how many .NET Runtimes you want to run the benchmarks against.

### Build

To build the benchmarks you need to have the right `dotnet cli`. This repository allows to benchmark .NET Core 2.0, 2.1, 2.2 and 3.0 so you need to install all of them.

- .NET Core 2.0, 2.1 and 2.2 can be installed from [https://www.microsoft.com/net/download/archives](https://www.microsoft.com/net/download/archives)
- .NET Core 3.0 preview is available at [https://github.com/dotnet/core-sdk#installers-and-binaries](https://github.com/dotnet/core-sdk#installers-and-binaries)

If you don't want to install all of them and just run the benchmarks for selected runtime(s), you need to manually edit the [common.props](../src/benchmarks/micro/common.props) file.

```diff
-     <TargetFrameworks>netcoreapp2.1;netcoreapp2.2;netcoreapp3.0</TargetFrameworks>
+     <TargetFrameworks>netcoreapp2.1</TargetFrameworks>
```

Once you have it, you can build the [MicroBenchmarks](../src/benchmarks/micro/MicroBenchmarks.csproj) project. Please do remember that the default configuration for `dotnet cli` is `Debug`. Running benchmarks in `Debug` makes no sense, so please **always build and run it in `Release` mode**.

```cmd
cd performance\src\benchmarks\micro
dotnet build -c Release
```

## Choosing the benchmarks

The project contains few thousands of benchmarks and you most probably don't need to run all of them.

The benchmarks are:

- grouped into corresponding namespaces. If given .NET class belongs to `System.XYZ` namespace, it's benchmarks do so as well.
- grouped into categories. CoreCLR benchmarks belong to `CoreCLR` category, CoreFX to `CoreFX` category. Features that are partially implemented in the Runtime and Base Class Library belong to common categories like `Span`, `LINQ`. (See [Categories.cs](../src/benchmarks/micro/Categories.cs) for more).

The harness (BenchmarkDotNet) allows to:

- filter the benchmarks by using glob expression applied to their full names (`namespace.typeName.methodName(arguments)`). This is exposed by `--filter` or just `-f` command line argument.
- filter the benchmarks by categories. This is exposed by `--allCategories` and `--anyCategories` command line arguments.
- print a list of available benchmarks by using `--list flat` or `--list tree` command line arguments.
- `--list` can be  combined with `--filter` and (`--allCategories` or `--anyCategories`).

The best way to find the benchmarks you want to run is either to open [MicroBenchmarks.sln](../src/benchmarks/micro/MicroBenchmarks.sln) in your favourite IDE and search for type usages or use command line arguments to filter. We expect that our users will start with the IDE approach and over the time switch to console line arguments once they got used to existing conventions.

### Filtering Examples

Sample commands:

- See the list of all available .NET Core 2.1 System.Linq benchmarks:

```cmd
dotnet run -c Release -f netcoreapp2.1 -- -f System.Linq* --list flat
```

```log
System.Linq.Tests.Perf_Linq.Select
System.Linq.Tests.Perf_Linq.SelectSelect
System.Linq.Tests.Perf_Linq.Where
System.Linq.Tests.Perf_Linq.WhereWhere
System.Linq.Tests.Perf_Linq.WhereSelect
System.Linq.Tests.Perf_Linq.Cast_ToBaseClass
System.Linq.Tests.Perf_Linq.Cast_SameType
System.Linq.Tests.Perf_Linq.OrderBy
System.Linq.Tests.Perf_Linq.OrderByDescending
System.Linq.Tests.Perf_Linq.OrderByThenBy
System.Linq.Tests.Perf_Linq.Reverse
System.Linq.Tests.Perf_Linq.Skip
System.Linq.Tests.Perf_Linq.Take
System.Linq.Tests.Perf_Linq.SkipTake
System.Linq.Tests.Perf_Linq.ToArray
System.Linq.Tests.Perf_Linq.ToList
System.Linq.Tests.Perf_Linq.ToDictionary
System.Linq.Tests.Perf_Linq.Contains_ElementNotFound
System.Linq.Tests.Perf_Linq.Contains_FirstElementMatches
System.Linq.Tests.Perf_Linq.Range
```

- See a hierarchy tree of all available .NET Core 3.0 System.IO.Compression benchmarks:

```cmd
dotnet run -c Release -f netcoreapp3.0 -- -f System.IO.Compression* --list tree
```

```log
System
 └─IO
    └─Compression
       ├─Brotli
       │  ├─Compress_WithState
       │  ├─Decompress_WithState
       │  ├─Compress_WithoutState
       │  ├─Decompress_WithoutState
       │  ├─Compress
       │  └─Decompress
       ├─Deflate
       │  ├─Compress
       │  └─Decompress
       └─Gzip
          ├─Compress
          └─Decompress
```

- See a list of all the benchmarks which belong to BenchmarksGame category:

```cmd
dotnet run -c Release -f netcoreapp2.1 -- --allCategories BenchmarksGame --list flat
```

```log
BenchmarksGame.BinaryTrees_2.RunBench
BenchmarksGame.BinaryTrees_5.RunBench
BenchmarksGame.FannkuchRedux_2.RunBench
BenchmarksGame.FannkuchRedux_5.RunBench
BenchmarksGame.Fasta_1.RunBench
BenchmarksGame.Fasta_2.RunBench
BenchmarksGame.KNucleotide_1.RunBench
BenchmarksGame.KNucleotide_9.RunBench
BenchmarksGame.Mandelbrot_2.Bench
BenchmarksGame.MandelBrot_7.Bench
BenchmarksGame.NBody_3.RunBench
BenchmarksGame.PiDigits_3.RunBench
BenchmarksGame.RegexRedux_1.RunBench
BenchmarksGame.RegexRedux_5.RunBench
BenchmarksGame.ReverseComplement_1.RunBench
BenchmarksGame.ReverseComplement_6.RunBench
BenchmarksGame.SpectralNorm_1.RunBench
BenchmarksGame.SpectralNorm_3.RunBench
```

### Benchmark not found

**If there are no benchmarks for the feature that you want to measure, you should write new ones following the existing guidelines.**

## Running the benchmarks

### Against single runtime

Just specify the target framework moniker for `dotnet run`.

Example: run `System.Collections.CopyTo<Int32>.Array` benchmarks against .NET Core 2.1 installed on your machine:

```cmd
dotnet run -c Release -f netcoreapp2.1 -- -f System.Collections.CopyTo<Int32>.Array
```

### Against multiple runtimes

You need to specify the target framework monikers via `--runtimes` or just `-r` option:

Example: run `System.Collections.CopyTo<Int32>.Array` benchmarks against .NET Core 2.0 and 2.1 installed on your machine:

```cmd
dotnet run -c Release -f netcoreapp2.1 -- -f System.Collections.CopyTo<Int32>.Array --runtimes netcoreapp2.1
```

**Important:** when comparing few different .NET runtimes please always use the lowest common API denominator as the host process. What does it mean? BDN needs to detect and build these benchmarks. If you run the host process as .NET Core 2.1 it won't be able to detect benchmarks that use newer APIs are available only for .NET Core 3.0.

Example: run benchmarks for APIs available in .NET Core 2.1 using .NET Core 3.0

```cmd
dotnet run -c Release -f netcoreapp2.1 -- -r netcoreapp3.0
```

Example: run benchmarks for APIs available in .NET Core 3.0 using .NET Core 3.0

```cmd
dotnet run -c Release -f netcoreapp3.0
```

### Against single runtime using given dotnet cli

Specify the target framework moniker for `dotnet run` and the path to `dotnet cli` via `--cli` argument.

Example: run `System.Collections.CopyTo<Int32>.Array` benchmarks against .NET Core 3.0 downloaded to a given location:

```cmd
dotnet run -c Release -f netcoreapp3.0 -- -f System.Collections.CopyTo<Int32>.Array --cli C:\tmp\dotnetcli\dotnet.exe
```

### Against private runtime build

Pass the path to CoreRun using `--coreRun` argument. In both CoreCLR and CoreFX you are going to find few CoreRun.exe files. **Use the one that has framework assemblies in the same folder**. Examples:

- "C:\Projects\coreclr\bin\tests\Windows_NT.x64.Release\Tests\Core_Root\CoreRun.exe"
- "C:\Projects\corefx\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe"

**Note:** BenchmarkDotNet expects a path to `CoreRun.exe` file (`corerun` on Unix), not to `Core_Root` folder.

Example: Run all CoreCLR benchmarks using "C:\Projects\corefx\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe"

```cmd
dotnet run -c Release -f netcoreapp3.0 -- --allCategories CoreCLR --coreRun "C:\Projects\corefx\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe"
```

If you want to use some non-default dotnet cli (or you just don't have a default dotnet cli) to build the benchmarks pass the path to cli via `--cli`.
If you want restore the packages to selected folder, pass it via `--packages`.

Example: Run all CoreCLR benchmarks using "C:\Projects\coreclr\bin\tests\Windows_NT.x64.Release\Tests\Core_Root\CoreRun.exe", restore the packages to C:\Projects\coreclr\packages and use "C:\Projects\coreclr\Tools\dotnetcli\dotnet.exe" for building the benchmarks.

```cmd
dotnet run -c Release -f netcoreapp3.0 -- --allCategories CoreCLR --coreRun "C:\Projects\coreclr\bin\tests\Windows_NT.x64.Release\Tests\Core_Root\CoreRun.exe --cli "C:\Projects\coreclr\Tools\dotnetcli\dotnet.exe" --packages "C:\Projects\coreclr\packages"
```

**VERY IMPORTANT**: CoreRun is a simple host that does NOT take any dependency on NuGet. BenchmarkDotNet just generates some boilerplate code, builds it and tells CoreRun.exe to run the benchmarks from the auto-generated library. CoreRun runs the benchmarks using the libraries that are placed in it's folder. When benchmarked code has a dependency to `System.ABC.dll` version 4.5 and CoreRun has `System.ABC.dll` version 4.5.1 in it's folder, then CoreRun is going to load and use `System.ABC.dll` version 4.5.1. This is why having a single clone of .NET Performance repository allows you to run benchmarks against private builds of CoreCLR/FX from many different locations.

## Workflows

Make sure you read [Pre-requisites](#Pre-requisites), [Choosing the benchmarks](#Choosing-the-benchmarks) and [Running the benchmarks](#Running-the-benchmarks) first!!

To get the best of available tooling you should also read:

- [How to read the Memory Statistics](../src/benchmarks/micro/README.md#How-to-read-the-Memory-Statistics)
- [How to get the Disassembly](../src/benchmarks/micro/README.md#How-to-get-the-Disassembly)
- [How to profile benchmarked code using ETW](../src/benchmarks/micro/README.md#How-to-profile-benchmarked-code-using-ETW)

By using the `DisassemblyDiagnoser` and `EtwProfiler` you should be able to get full disassembly and detailed profile information. No code modification is required, every feature is available from command line level!

### Improving the performance

1. [Choose the benchmarks](#Choosing-the-benchmarks) that test given feature.
2. Run the benchmarks without your changes first
   1. clone your fork of selected product repo - CoreFX/CoreCLR
   2. build it in release, including tests
   3. locate the path to **right** `CoreRun.exe` (the  one with all framework dependencies next to it)
   4. go to the folder with benchmarks (`cd dotnet/performance/src/benchmarks/micro`)
   5. run the benchmarks using given `CoreRun.exe` and save the results to a dedicated folder. An example:

        ```cmd
        dotnet run -c Release -f netcoreapp3.0 --
            --artifacts before
            --filter *Span*
            --coreRun "C:\Projects\corefx\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe"
        ```

3. Use at least one of the available BenchmarkDotNet features: `MemoryDiagnoser`, `DisassemblyDiagnoser` or `EtwProfiler` to get more performance data.
4. Analyze the data, identify performance bottlenecks.
5. Apply your changes to fix the performance bottlenecks.
6. Rebuild the product and tests in `Release` mode. Verify that the modified files got copied to the folder with `CoreRun`.
7. Run the benchmarks using given `CoreRun.exe` and save the results to a dedicated folder. **Different one that you used to store results previously!** Example:

    ```cmd
    dotnet run -c Release -f netcoreapp3.0 --  --artifacts after --filter *Span* --coreRun "C:\Projects\corefx\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe"
    ```

8. Compare the results using [Results Comparer](../src/tools/ResultsComparer/README.md)

    ```cmd
    dotnet run -p ..\..\tools\ResultsComparer\ResultsComparer.csproj --base .\before\ --diff .\after\ --threshold 3%
    ```

9. Repeat steps 3-8 until you get the desired speedup.

### Checking for regressions using Statistical Test

To perform a Mann–Whitney U Test and display the results in a dedicated column you need to provide the threshold for statistical test (Examples: 5%, 10ms, 100ns, 1s):

    `--statisticalTest <THRESHOLD>`

Example: run Mann–Whitney U test with an absolute ratio of 3 milliseconds and compare the CoreFX located in `C:\Projects\corefx_upstream\` vs `C:\Projects\corefx_fork\` for BenchmarksGame benchmarks.

The following commands are represented in a few lines to make it easier to read on GitHub. Please remove the new lines when copy-pasting to console.

```cmd
dotnet run -c Release -f netcoreapp3.0 \
    --allCategories BenchmarksGame \
    --statisticalTest 3ms \
    --coreRun \
        "C:\Projects\corefx_upstream\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe" \
        "C:\Projects\corefx_fork\bin\runtime\netcoreapp-Windows_NT-Release-x64\CoreRun.exe"
```

Example: run all benchmarks for .NET Core 2.1 vs 2.2 and use Mann–Whitney U test with a relative ratio of 5%.

```cmd
dotnet run -c Release -f netcoreapp2.1 --
    --filter *
    --statisticalTest 5%
    --runtimes netcoreapp2.1 netcoreapp2.2
```

**Note:** some of our benchmarks are dependent on the alignment. You can use `--launchCount` option to specify how many processes BenchmarkDotNet should start to run given benchmarks. BDN does it sequentially. It's recommended to use this feature when releasing a new version of .NET Framework.

**Note:** you don't need two copies of CoreCLR/FX to compare the performance. But in that case, you have to run the benchmarks at least once before applying any changes. To compare the historical results you need to use [Results Comparer](../src/tools/ResultsComparer/README.md)

### Comparing different runtimes

A must read is [running benchmarks against multiple runtimes](#Against-multiple-runtimes). All you need to do is to specify the runtime name and path to cli (if required).

Example: run all `Span` benchmarks for .NET Core 2.1 vs 3.0:

```cmd
dotnet run -c Release -f netcoreapp2.1 -- --allCategories Span --runtimes netcoreapp2.1 netcoreapp3.0
```

Example: run all `System.IO` benchmarks for .NET 4.7.2 vs .NET Core 3.0 preview using dotnet cli from given location:

```cmd
dotnet run -c Release -f net472 -- --filter System.IO* --runtimes net472 netcoreapp3.0 --cli "C:\Downloads\3.0.0-preview1-03129-01\dotnet.exe"
```

Example: run all benchmarks for .NET Core 2.1 vs 2.2:

```cmd
dotnet run -c Release -f netcoreapp2.1 -- -f * --runtimes netcoreapp2.1 netcoreapp2.2
```

### New API

In case you want to add a new method to CoreFX and test its performance, then you need to follow [Benchmarking .NET Core applications](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/benchmarking.md).

### Troubleshooting

BenchmarkDotNet uses MSBuild to build the auto-generated project with benchmarks. If you ever get following error:

```log
/home/username/dotnet30/sdk/3.0.100-preview-009750/Microsoft.Common.CurrentVersion.targets(4551,5): warning MSB3026: Could not copy "/home/username/git/performance/src/benchmarks/micro/obj/Release/netcoreapp3.0/MicroBenchmarks" to "bin/Release/netcoreapp3.0/MicroBenchmarks". Beginning retry 1 in 1000ms. Text file busy  [/home/username/git/performance/src/benchmarks/micro/MicroBenchmarks.csproj]
```

Please:

1. Try to re-run the command once.
2. If re-run does not help, please remove `bin` and `obj` folders and try again.
3. If it still does not work, follow [Troubleshooting guidelines](https://benchmarkdotnet.org/articles/guides/troubleshooting.html)
