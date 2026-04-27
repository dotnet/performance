# Benchmarks_local.py

This is a script for testing the performance of the different dotnet/runtime build types locally.

## General Code Flow

1. For each commit value specified
   1. Get the repo to the proper commit
   2. Build the dependencies for the run types specified and copy them to the artifact storage path
2. For each run type specified, run BenchmarkDotNet using the generated artifacts.
   - Note: Certain run types are run with all commits at once and some are separated out to run each runtype-commit pair individually. This is automatically selected for each runtype, but impacts the output artifact layout.

## Prerequisites

- Normal prereqs for building the target runtime: [Runtime Build Requirements](https://github.com/dotnet/runtime/blob/main/docs/workflow/README.md#Build_Requirements)
- Python 3
- gitpython (pip install --global gitpython)
- Ubuntu Version 22.04 if using Ubuntu
- May need llvm+clang 16 for MonoAOTLLVM (https://apt.llvm.org/)
- Wasm runs need jsvu/v8 installed and setup. Latest v8 preferred. (No need to setup EMSDK, the tool does that automatically when building)

## Commandline Options

The commandline options can be thought of as fitting into two groups:

- Arguments for changing the script specific functionality
- Arguments for changing the Runtime Artifact Dependency Generation and BenchmarkDotNet

Regardless, all arguments are passed into the commandline at the top level in the same way. The separation above is useful for understanding what portion of the code the argument impacts and understanding potential arguments to reference when adding new arguments to the code.

- Note: The actual options might vary depending on the specific implementation of the benchmarks_local.py script. Always refer to the script's source code or use the --help command to get the most accurate and up-to-date information.

### Script Specific Options

This group of arguments includes those that impact the setup and high-level flow of the script. This includes things such as which commits to test, where to store artifact caches, and whether to force the rerun of certain steps. Specific options include:

| Option                         | Description                                                                                      | Default                                                      | Options                                                                              |
| ------------------------------ | ------------------------------------------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------------------------------ |
| `--list-cached-builds`         | Lists the cached builds located in the artifact-storage-path.                                    | `False`                                                      |                                                                                      |
| `--commits`                    | The commits to test.                                                                             | No default value                                             | Any commit available at the repo-url. Ex. `dd079f53b95519c8398d8b0c6e796aaf7686b99a` |
| `--repo-url`                   | The runtime repo to test from, used to get data for a fork.                                      | 'https://github.com/dotnet/runtime.git'                      | Any reachable runtime repository                                                     |
| `--local-test-repo`            | Path to a local repo with the runtime source code to test from.                                  | No default value                                             | Any path to a runtime repo. Ex. `./path/to/folder/runtime`                           |
| `--separate-repos`             | Whether to test each runtime version from their own separate repo directory.                     | `False`                                                      |                                                                                      |
| `--repo-storage-path`          | The path to store the cloned repositories in.                                                    | Current working directory                                    | Any path to a directory                                                              |
| `--artifact-storage-path`      | The path to store the artifacts in (builds, results, etc).                                       | `runtime-testing-artifacts` in the current working directory | Any path to a directory                                                              |
| `--rebuild-artifacts`          | Whether to rebuild the artifacts for the specified commits before benchmarking.                  | `False`                                                      |                                                                                      |
| `--reinstall-dotnet`           | Whether to reinstall dotnet for use in building the benchmarks before running the benchmarks.    | `False`                                                      |                                                                                      |
| `--build-only`                 | Whether to only build the artifacts for the specified commits and not run the benchmarks.        | `False`                                                      |                                                                                      |
| `--skip-local-rebuild`         | Whether to skip rebuilding the local repo and use the already built version (if already built).  | `False`                                                      |                                                                                      |
| `--allow-non-admin-execution`  | Whether to allow non-admin execution of the script.                                              | `False`                                                      |                                                                                      |
| `--dont-kill-dotnet-processes` (deprecated) | This is now the default and is no longer needed. It is kept for backwards compatibility. | `False`                                                      |                                                                                      |
| `--kill-dotnet-processes`      | Whether to kill any dotnet processes throughout the script. This is useful for solving certain issues during builds due to mbsuild node reuse but kills all machine dotnet processes. (Note: This indirectly conflicts with --enable-msbuild-node-reuse as this should kill the nodes.) | `False` |  |
| `--enable-msbuild-node-reuse`  | Whether to enable MSBuild node reuse. This is useful for speeding up builds, but may cause issues with some builds, especially between different commits. (Note: This indirectly conflicts with --kill-dotnet-processes as killing the processes should kill the nodes.) | `False`  |  |
| `--run-types`                  | The types of runs to perform.                                                                    | No default value                                             | Names of the RunType enum values, View list via --help                               |
| `--quiet`                      | Whether to not print verbose output.                                                             | `False`                                                      |                                                                                      |

### Dependency Generation and BenchmarkDotNet options

This group of arguments includes those that have a direct impact on the runtime generated artifacts or BenchmarkDotNet runs. This includes things such as the architecture to target, the benchmark filter to run, and the csproj to run for the microbenchmarks. Specific options include:

| Option                 | Description                                                                                                                       | Default                                          | Options                                                            |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ | ------------------------------------------------------------------ |
| `--bdn-arguments`      | Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes. Must be passed like --bdn-arguments="--arg1 --arg2..." | No default value                                 |                                                                    |
| `--architecture`       | Specifies the SDK processor architecture.                                                                                         | The current systems architecture                 | 'x64', 'x86', 'arm64', 'arm'                                       |
| `--os`                 | Specifies the operating system of the system. Darwin is OSX.                                                                      | The option for the current OS                    | 'windows', 'linux', 'osx'                                          |
| `--filter`             | Specifies the benchmark filter to pass to BenchmarkDotNet.                                                                        | No default value                                 |                                                                    |
| `-f`, `--framework`    | The target framework used to build the microbenchmarks.                                                                           | 'net9.0'                                         | View list via --help                                               |
| `--csproj`             | The path to the csproj file to run benchmarks against.                                                                            | "../src/benchmarks/micro/MicroBenchmarks.csproj" | Any path to a BenchmarkDotNet project                              |
| `--mono-libclang-path` | The full path to the clang compiler to use for the benchmarks. Used for "MonoLibClang" build property.                            | No default value                                 | Path to local clang compiler. e.g. `/usr/local/lib/libclang.so.16` |
| `--wasm-engine-path`   | The full path to the wasm engine to use for the benchmarks. Required for WasmInterpreter and WasmAOT RunTypes                     | No default value                                 | Path to wasm engine to use. e.g. `/usr/local/bin/v8`.              |

## Usage Examples

Here is an example command line that runs the MonoJIT RunType from a local runtime for the tests matching `*Span.IndexerBench.CoveredIndex2*`:

`python .\benchmarks_local.py --local-test-repo "<absolute path to runtime folder>/runtime" --run-types MonoJIT --filter *Span.IndexerBench.CoveredIndex2*`

Here is an example command line that runs the MonoInterpreter and MonoJIT RunTypes using commits `dd079f53` and `69702c37` for the tests `*Span.IndexerBench.CoveredIndex2*` with the commits being cloned to the `--repo-storage-path` for building, it also passes `--join` to BenchmarkDotNet so all the reports from a single run will be joined into a single report:

`python .\benchmarks_local.py --commits dd079f53b95519c8398d8b0c6e796aaf7686b99a 69702c372a051580f76defc7ba899dde8fcd2723 --repo-storage-path "<absolute path to where you want to store runtime clones>" --run-types MonoInterpreter MonoJIT --filter *Span.IndexerBench.CoveredIndex2* *WriteReadAsync* --bdn-arguments="--join"`

- Note: There is not currently a way to block specific RunTypes from being run on specific hardware.

## Useful Microbenchmark Filters

Below is a table of filter aliases that are often affected by regressions and may act as a good subset for detecting regressions. These can be used by passing the filter directly into the `--filter` argument. If you have your own filter that is useful, please open a PR so we can add it to the list!

| RunType             | Filter             | Notes             |
| ------------------- | ------------------ | ----------------- |
| RunType PlaceHolder | Filter PlaceHolder | Notes PlaceHolder |

## Adding New RunTypes

1. Add the run type to the RunType enum
2. Add the build instructions to the generate_all_runtype_dependencies function
   - If the build steps match an already created build flow, you can just add the run type to the matching flow and add the new Runtype to the paths the file is copied to.
3. Add the BenchmarkDotNet run arguments to the generate_combined_benchmark_ci_args and generate_single_benchmark_ci_args function
    - If the BenchmarkDotNet toolchain for that runtype supports multiple runtimes, add the arguments for running all of the runtimes at the same time to generate_combined_benchmark_ci_args, otherwise return a TypeError stating that combined does not support that RunType to ensure we never accidentally try to run that RunType with the runtimes combined.
    - All RunTypes should have argument generation setup in generate_single_benchmark_ci_args for a single runtime of that RunType to future proof for the ability to run all runtype-commit pairs individually.
4. Add the RunType to the RunType list in the run_benchmarks definition based on whether the RunType toolchain supports combined runtime execution or only single execution.  
5. Verify that the flow runs successfully locally, and preferably also test on other machines/OSes.
