# Prerequisites

## Clone

This repository is **independent from CoreFX and CoreCLR repositories!**  So this is the only repository you need to clone.

```cmd
git clone https://github.com/dotnet/performance.git
```

## Build

To build the benchmarks you need to have the right `dotnet cli`. This repository allows to benchmark .NET Core 2.0, 2.1, 2.2 and 3.0 so you need to install all of them.

- .NET Core 2.0, 2.1 and 2.2 can be installed from [https://www.microsoft.com/net/download/archives](https://www.microsoft.com/net/download/archives)
- .NET Core 3.0 preview is available at [https://github.com/dotnet/core-sdk#installers-and-binaries](https://github.com/dotnet/core-sdk#installers-and-binaries)

If you don't want to install all of them and just run the benchmarks for selected runtime(s), you need to manually edit the [common.props](../build/common.props) file.

```diff
-<TargetFrameworks>netcoreapp2.0;netcoreapp2.1;netcoreapp2.2;netcoreapp3.0</TargetFrameworks>
+<TargetFrameworks>netcoreapp3.0</TargetFrameworks>
```

## Alternative: Python script

If you don't want to install `dotnet cli` manually, we have a Python 3 script which can do that for you. All you need to do is to provide the frameworks:

```cmd
py .\scripts\benchmarks_ci.py --frameworks netcoreapp3.0
```

