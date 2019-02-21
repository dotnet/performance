# Prerequisites

## Clone

This repository is **independent of CoreFX and CoreCLR repositories!**  So this is the only repository you need to clone.

```cmd
git clone https://github.com/dotnet/performance.git
```

## External tools

### [python](https://www.python.org/)

Python is needed to run the scripts used by automation. These scripts wrap all the logic for tool acquisition, benchmarks build and execution, data collection and upload.

The python scripts in this repository support python version 3.5 or greater.

- [Downloads](https://www.python.org/downloads/)

### [.NET Core command-line interface (CLI) tools](https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x)

Used to build the .NET Performance projects, and they can be downloaded here:

- [Downloads](https://dotnet.microsoft.com/download)

Optionally, you could use [dotnet.py](../scripts/dotnet.py) to to download the DotNet Cli locally.
