# Results Comparer

This simple tool allows for easy comparison of provided benchmark results.

It can be used to compare:

* historical results (eg. before and after my changes)
* results for different OSes (eg. Windows vs Ubuntu)
* results for different CPU architectures (eg. x64 vs ARM64)
* results for different target frameworks (eg. .NET Core 3.1 vs 5.0)

All you need to provide is:

* `--base` - path to folder/file with baseline results
* `--diff` - path to folder/file with diff results
* `--threshold`  - threshold for Statistical Test. Examples: 5%, 10ms, 100ns, 1s

Optional arguments:

* `--top` - filter the diff to top/bottom `N` results
* `--noise` - noise threshold for Statistical Test. The difference for 1.0ns and 1.1ns is 10%, but it's just a noise. Examples: 0.5ns 1ns. The default value is 0.3ns.
* `--csv` - path to exported CSV results. Optional.
* `-f|--filter` - filter the benchmarks by name using glob pattern(s). Optional.

Sample: compare the results stored in `C:\results\windows` vs `C:\results\ubuntu` using `1%` threshold and print only TOP 10.

```cmd
dotnet run --base "C:\results\windows" --diff "C:\results\ubuntu" --threshold 1% --top 10
```

**Note**: the tool supports only `*full.json` results exported by BenchmarkDotNet. This exporter is enabled by default in this repository.

## Sample results

| Slower                                                          | diff/base | Base Median (ns) | Diff Median (ns) | Modality|
| --------------------------------------------------------------- | ---------:| ----------------:| ----------------:| -------:|
| PerfLabTests.BlockCopyPerf.CallBlockCopy(numElements: 100)      |      1.60 |             9.22 |            14.76 |         |
| System.Tests.Perf_String.Trim_CharArr(s: "Test", c: [' ', ' ']) |      1.41 |             6.18 |             8.72 |         |

| Faster                              | base/diff | Base Median (ns) | Diff Median (ns) | Modality|
| ----------------------------------- | ---------:| ----------------:| ----------------:| -------:|
| System.Tests.Perf_Array.ArrayCopy3D |      1.31 |           372.71 |           284.73 |         |

If there is no difference or if there is no match (we use full benchmark names to match the benchmarks), then the results are omitted.

## Matrix

The tools supports also comparing multiple result sets. For up-to-date help please run `dotnet run -- matrix --help`.

Sample usage:

```cmd
dotnet run -c Release matrix decompress --input D:\results\Performance-Runs.zip --output D:\results\net7.0-preview3
dotnet run -c Release matrix --input D:\results\net7.0-preview3 --base net7.0-preview2 --diff net7.0-preview3 --threshold 10% --noise 2ns --filter 'System.IO*'
```

Sample results:

## System.IO.Tests.Perf_File.WriteAllText(size: 10000)

| Result |       Base |       Diff | Ratio | Alloc Delta | Modality | Operating System      | Bit   | Processor Name                                  | Base V       | Diff V      |
| ------ | ----------:| ----------:| -----:| -----------:| -------- | --------------------- | ----- | ----------------------------------------------- | ------------ | ------------ |
| Same   |  939321.02 | 1031195.70 |  0.91 |          +0 | several? | Windows 10            | X64   | Intel Xeon CPU E5-1650 v4 3.60GHz               | 7.0.22.12204 | 7.0.22.17504|
| Faster | 1059005.27 |  598518.92 |  1.77 |          +0 | bimodal  | Windows 11            | X64   | AMD Ryzen Threadripper PRO 3945WX 12-Cores      | 7.0.22.12204 | 7.0.22.17504|
| Faster |  937008.80 |  551313.28 |  1.70 |          +0 | several? | Windows 11            | X64   | AMD Ryzen 9 5900X                               | 7.0.22.12204 | 7.0.22.17504|
| Faster | 4346259.38 | 3206257.03 |  1.36 |          +0 | several? | Windows 11            | X64   | Intel Core i5-4300U CPU 1.90GHz (Haswell)       | 7.0.22.12204 | 7.0.22.17504|
| Faster | 2573217.71 |  832166.18 |  3.09 |          -6 |          | Windows 11            | X64   | Unknown processor                               | 7.0.22.12204 | 7.0.22.17504|
| Same   |  235188.35 |  217942.50 |  1.08 |          +0 |          | Windows 11            | X64   | Intel Core i7-8700 CPU 3.20GHz (Coffee Lake)    | 7.0.22.12204 | 7.0.22.17504|
| Same   |  824210.94 |  749032.29 |  1.10 |          +1 |          | Windows 11            | X64   | Intel Core i9-9900T CPU 2.10GHz                 | 7.0.22.12204 | 7.0.22.17504|
| Same   |   50128.53 |   50988.47 |  0.98 |          +0 |          | alpine 3.13           | X64   | Intel Core i7-7700 CPU 3.60GHz (Kaby Lake)      | 7.0.22.12204 | 7.0.22.17504|
| Same   |   79680.16 |   78657.24 |  1.01 |          +0 |          | centos 7              | X64   | Intel Xeon CPU E5530 2.40GHz                    | 7.0.22.12204 | 7.0.22.17504|
| Same   |   48132.14 |   48840.28 |  0.99 |          +0 |          | debian 11             | X64   | Intel Core i7-7700 CPU 3.60GHz (Kaby Lake)      | 7.0.22.12204 | 7.0.22.17504|
| Same   |   42636.21 |   44366.44 |  0.96 |          +0 | several? | pop 20.04             | X64   | Intel Core i7-6600U CPU 2.60GHz (Skylake)       | 7.0.22.12204 | 7.0.22.17504|
| Same   |   32762.42 |   32443.19 |  1.01 |          +0 | bimodal  | ubuntu 18.04          | X64   | Intel Xeon CPU E5-1650 v4 3.60GHz               | 7.0.22.12204 | 7.0.22.17504|
| Faster |   64744.24 |   55839.56 |  1.16 |          +0 | bimodal  | ubuntu 18.04          | X64   | Intel Core i7-2720QM CPU 2.20GHz (Sandy Bridge) | 7.0.22.12204 | 7.0.22.17504|
| Same   | 3684335.97 | 3726101.03 |  0.99 |          +0 |          | alpine 3.12           | Arm64 | Unknown processor                               | 7.0.22.12204 | 7.0.22.17504|
| Same   |   60851.89 |   57414.92 |  1.06 |          +0 |          | debian 11             | Arm64 | Unknown processor                               | 7.0.22.12204 | 7.0.22.17504|
| Same   |   84304.48 |   83274.12 |  1.01 |          +0 |          | ubuntu 18.04          | Arm64 | Unknown processor                               | 7.0.22.12204 | 7.0.22.17504|
| Faster | 2489377.68 |  515978.13 |  4.82 |          -5 |          | Windows 10            | Arm64 | Microsoft SQ1 3.0 GHz                           | 7.0.22.12204 | 7.0.22.17504|
| Faster | 2675980.21 |  939078.31 |  2.85 |          -5 |          | Windows 11            | Arm64 | Microsoft SQ1 3.0 GHz                           | 7.0.22.12204 | 7.0.22.17504|
| Faster | 1158829.33 |  469372.13 |  2.47 |          -1 |          | Windows 10            | X86   | Intel Xeon CPU E5-1650 v4 3.60GHz               | 7.0.22.12204 | 7.0.22.17504|
| Faster |  929645.42 |  507981.70 |  1.83 |          -2 | bimodal  | Windows 11            | X86   | AMD Ryzen Threadripper PRO 3945WX 12-Cores      | 7.0.22.12204 | 7.0.22.17504|
| Faster | 3215358.93 |  440157.77 |  7.31 |          -6 |          | Windows 11            | X86   | Intel Core i7-10510U CPU 1.80GHz                | 7.0.22.12204 | 7.0.22.17504|
| Same   |  126829.97 |  121465.99 |  1.04 |          +0 |          | Windows 7 SP1         | X86   | Intel Core i7-7700 CPU 3.60GHz (Kaby Lake)      | 7.0.22.12204 | 7.0.22.17504|
| Same   |  218819.23 |  214187.24 |  1.02 |          -1 | bimodal  | ubuntu 18.04          | Arm   | ARMv7 Processor rev 3 (v7l)                     | 7.0.22.12204 | 7.0.22.17504|
| Faster | 2478265.18 |  547273.17 |  4.53 |          -5 |          | Windows 10            | Arm   | Microsoft SQ1 3.0 GHz                           | 7.0.22.12204 | 7.0.22.17504|
| Same   |  161909.04 |  158812.34 |  1.02 |          +0 |          | macOS Monterey 12.2.1 | X64   | Intel Core i7-5557U CPU 3.10GHz (Broadwell)     | 7.0.22.12204 | 7.0.22.17504|
| Same   |  121620.87 |  122424.61 |  0.99 |          +0 |          | macOS Monterey 12.3.1 | X64   | Intel Core i7-4870HQ CPU 2.50GHz (Haswell)      | 7.0.22.12204 | 7.0.22.17504|
