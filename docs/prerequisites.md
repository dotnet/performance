# Prerequisites

## Clone

This repository is **independent of [dotnet/runtime](https://github.com/dotnet/runtime) repository!**  So this is the only repository you need to clone.

```cmd
git clone https://github.com/dotnet/performance.git
```

## External tools

### [python](https://www.python.org/)

Python is needed to run the scripts used by automation. These scripts wrap all the logic for tool acquisition, benchmarks build and execution, data collection and upload.

The python scripts in this repository support python version 3.5 or greater.

- [Downloads](https://www.python.org/downloads/)

### .NET Core SDK

The .NET Core SDK contains both the .NET Core runtime and CLI tools. .NET Performance projects test the performance of daily builds of .NET Core Runtime. **You need to install the latest daily build of .NET Core SDK to be able to build the projects**. It can be downloaded here:

- [Downloads](https://github.com/dotnet/core-sdk#installers-and-binaries)

Optionally, you could use [dotnet.py](../scripts/dotnet.py) to to download the DotNet Cli locally.
