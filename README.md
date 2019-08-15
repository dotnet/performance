# .NET Performance

| Public Build Status                         | Internal Build Status                           |
| :------------------------------------------ | :---------------------------------------------: |
| [![public_build_icon]][public_build_status] | [![internal_build_icon]][internal_build_status] |

This repo contains benchmarks used for testing the performance of all .NET Runtimes: .NET Core, Full .NET Framework, Mono and CoreRT.

Finding these benchmarks in a separate repository might be surprising. Performance in a given scenario may be impacted by changes in seemingly unrelated components. Using this central repository ensures that measurements are made in comparable ways across all .NET runtimes and repos. This consistency lets engineers make progress and ensures the customer scenarios are protected.

## Documentation

* [Microbenchmarks Guide](./src/benchmarks/micro/README.md) for information on running our microbenchmarks
* [Real-World Scenarios Guide](./src/benchmarks/real-world/JitBench/README.md) for information on running our real-world scenario benchmarks
* [Benchmarking workflow for CoreFX](./docs/benchmarking-workflow-corefx.md) for information on working with CoreFX

## Contributing to Repository

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## Build Status

### Micro Benchmarks

| Framework | Windows RS4 x64                                                                             | Windows RS4 x86                                                                             | Ubuntu 16.04 x64                                                                            | Ubuntu 16.04 ARM64                                                                              |
| :-------- | :-----------------------------------------------------------------------------------------: | :-----------------------------------------------------------------------------------------: | :-----------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------: |
| Core 5.0  | [![micro_windows_RS4_x64_netcoreapp5.0_icon]][micro_windows_RS4_x64_netcoreapp5.0_status] | [![micro_windows_RS4_x86_netcoreapp5.0_icon]][micro_windows_RS4_x86_netcoreapp5.0_status] | [![micro_ubuntu_1604_x64_netcoreapp5.0_icon]][micro_ubuntu_1604_x64_netcoreapp5.0_status] | Disabled |
| Core 3.0  | [![micro_windows_RS4_x64_netcoreapp3.0_icon]][micro_windows_RS4_x64_netcoreapp3.0_status] | [![micro_windows_RS4_x86_netcoreapp3.0_icon]][micro_windows_RS4_x86_netcoreapp3.0_status] | [![micro_ubuntu_1604_x64_netcoreapp3.0_icon]][micro_ubuntu_1604_x64_netcoreapp3.0_status] | Disabled |
| Core 2.2  | [![micro_windows_RS4_x64_netcoreapp2.2_icon]][micro_windows_RS4_x64_netcoreapp2.2_status] |                                                                                             | [![micro_ubuntu_1604_x64_netcoreapp2.2_icon]][micro_ubuntu_1604_x64_netcoreapp2.2_status] | N/A                                                                                             |
| Core 2.1  | [![micro_windows_RS4_x64_netcoreapp2.1_icon]][micro_windows_RS4_x64_netcoreapp2.1_status] |                                                                                             | [![micro_ubuntu_1604_x64_netcoreapp2.1_icon]][micro_ubuntu_1604_x64_netcoreapp2.1_status] | N/A                                                                                             |
| .NET      | [![micro_windows_RS4_x64_net461_icon]][micro_windows_RS4_x64_net461_status]               |                                                                                             | N/A                                                                                         | N/A                                                                                             |

[//]: # (These are the repo links)

[public_build_icon]:                               https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master
[public_build_status]:                             https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master
[internal_build_icon]:                             https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/performance/dotnet-performance?branchName=master
[internal_build_status]:                           https://dev.azure.com/dnceng/internal/_build/latest?definitionId=306&branchName=master

### Real World Benchmarks

#### ML.NET

| Framework | Windows RS4 x64                                                                                 | Ubuntu 16.04 x64                                                                                |
| :-------- | :---------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------: |
| Core 3.0  | [![mldotnet_windows_RS4_x64_netcoreapp3.0_icon]][mldotnet_windows_RS4_x64_netcoreapp3.0_status] | [![mldotnet_ubuntu_1604_x64_netcoreapp3.0_icon]][mldotnet_ubuntu_1604_x64_netcoreapp3.0_status] |

#### Roslyn

| Framework | Windows RS4 x64                                                                             |
| :-------- | :-----------------------------------------------------------------------------------------: |
| Core 3.0  | [![roslyn_windows_RS4_x64_netcoreapp3.0_icon]][roslyn_windows_RS4_x64_netcoreapp3.0_status] |

[//]: # (These are the micro links)

[//]: # (These are the windows x64 links)
[micro_windows_RS4_x64_netcoreapp5.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp5.0
[micro_windows_RS4_x64_netcoreapp5.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp5.0
[micro_windows_RS4_x64_netcoreapp3.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp3.0
[micro_windows_RS4_x64_netcoreapp3.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp3.0
[micro_windows_RS4_x64_netcoreapp2.2_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp2.2
[micro_windows_RS4_x64_netcoreapp2.2_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp2.2
[micro_windows_RS4_x64_netcoreapp2.1_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp2.1
[micro_windows_RS4_x64_netcoreapp2.1_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64%20micro&configuration=windows%20RS4%20x64%20micro%20netcoreapp2.1
[micro_windows_RS4_x64_net461_status]:            https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64%20micro_net461
[micro_windows_RS4_x64_net461_icon]:              https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64%20micro_net461


[//]: # (These are the windows x86 links)
[micro_windows_RS4_x86_netcoreapp5.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86%20micro&configuration=windows%20RS4%20x86%20micro%20netcoreapp5.0
[micro_windows_RS4_x86_netcoreapp5.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86%20micro&configuration=windows%20RS4%20x86%20micro%20netcoreapp5.0
[micro_windows_RS4_x86_netcoreapp3.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86%20micro&configuration=windows%20RS4%20x86%20micro%20netcoreapp3.0
[micro_windows_RS4_x86_netcoreapp3.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86%20micro&configuration=windows%20RS4%20x86%20micro%20netcoreapp3.0

[//]: # (These are the ubuntu x64 links)
[micro_ubuntu_1604_x64_netcoreapp5.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp5.0
[micro_ubuntu_1604_x64_netcoreapp5.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp5.0
[micro_ubuntu_1604_x64_netcoreapp3.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp3.0
[micro_ubuntu_1604_x64_netcoreapp3.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp3.0
[micro_ubuntu_1604_x64_netcoreapp2.2_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp2.2
[micro_ubuntu_1604_x64_netcoreapp2.2_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp2.2
[micro_ubuntu_1604_x64_netcoreapp2.1_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp2.1
[micro_ubuntu_1604_x64_netcoreapp2.1_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64%20micro&configuration=ubuntu%201604%20x64%20micro%20netcoreapp2.1

[//]: # (These are the ubuntu arm64 links)
[micro_ubuntu_1604_arm64_netcoreapp3.0_status]:   https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20arm64%20micro&configuration=ubuntu%201604%20arm64%20micro%20netcoreapp3.0
[micro_ubuntu_1604_arm64_netcoreapp3.0_icon]:     https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20arm64%20micro&configuration=ubuntu%201604%20arm64%20micro%20netcoreapp3.0

[//]: # (These are the ML.NET links)

[//]: # (These are the windows x64 links)
[mldotnet_windows_RS4_x64_netcoreapp3.0_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64%20mlnet
[mldotnet_windows_RS4_x64_netcoreapp3.0_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64%20mlnet

[//]: # (These are the ubuntu x64 links)
[mldotnet_ubuntu_1604_x64_netcoreapp3.0_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64%20mlnet
[mldotnet_ubuntu_1604_x64_netcoreapp3.0_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64%20mlnet


[//]: # (These are the Roslyn links)

[//]: # (These are the windows x64 links)
[roslyn_windows_RS4_x64_netcoreapp3.0_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64%20roslyn
[roslyn_windows_RS4_x64_netcoreapp3.0_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64%20roslyn
