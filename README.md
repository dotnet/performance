# .NET Performance

| Build Source Version                        | Public Build Status                                                         | Internal Build Status                                                           |
| :------------------------------------------ | :-------------------------------------------------------------------------- | :------------------------------------------------------------------------------ |
| main                                        | [![public_build_icon_main]][public_build_status_main]                       | [![internal_build_icon_main]][internal_build_status_main]                       |
| release/7.0                                 | [![public_build_icon_release_7.0]][public_build_status_release_7.0]         | [![internal_build_icon_release_7.0]][internal_build_status_release_7.0]         |
| release/6.0                                 | [![public_build_icon_release_6.0]][public_build_status_release_6.0]         | [![internal_build_icon_release_6.0]][internal_build_status_release_6.0]         |

This repo contains benchmarks used for testing the performance of all .NET Runtimes: .NET Core, Full .NET Framework, Mono and CoreRT.

Finding these benchmarks in a separate repository might be surprising. Performance in a given scenario may be impacted by changes in seemingly unrelated components. Using this central repository ensures that measurements are made in comparable ways across all .NET runtimes and repos. This consistency lets engineers make progress and ensures the customer scenarios are protected.

## Documentation

* [Microbenchmarks Guide](./src/benchmarks/micro/README.md) for information on running our microbenchmarks
* [Scenario Tests Guide](./docs/scenarios-workflow.md) for information on running our real-world scenario benchmarks
* [Benchmarking workflow for dotnet/runtime repository](./docs/benchmarking-workflow-dotnet-runtime.md) for information on benchmarking local [dotnet/runtime](https://github.com/dotnet/runtime) builds
* [Profiling workflow for dotnet/runtime repository](./docs/profiling-workflow-dotnet-runtime.md) for information on profiling local [dotnet/runtime](https://github.com/dotnet/runtime) builds

## Contributing to Repository

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

[public_build_icon_main]:                        https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/performance/performance-ci?branchName=main
[public_build_status_main]:                      https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=38&branchName=main
[internal_build_icon_main]:                      https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/performance/dotnet-performance?branchName=main
[internal_build_status_main]:                    https://dev.azure.com/dnceng/internal/_build/latest?definitionId=306&branchName=main

[public_build_icon_release_7.0]:                 https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/performance/performance-ci?branchName=release%2F7.0
[public_build_status_release_7.0]:               https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=38&branchName=release%2F7.0
[internal_build_icon_release_7.0]:               https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/performance/dotnet-performance?branchName=release%2F7.0
[internal_build_status_release_7.0]:             https://dev.azure.com/dnceng/internal/_build/latest?definitionId=306&branchName=release%2F7.0

[public_build_icon_release_6.0]:                 https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/performance/performance-ci?branchName=release%2F6.0
[public_build_status_release_6.0]:               https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=38&branchName=release%2F6.0
[internal_build_icon_release_6.0]:               https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/performance/dotnet-performance?branchName=release%2F6.0
[internal_build_status_release_6.0]:             https://dev.azure.com/dnceng/internal/_build/latest?definitionId=306&branchName=release%2F6.0
