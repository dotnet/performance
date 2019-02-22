# .NET Performance

[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master)

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

#### Full_opt

| Framework | Windows RS4 x64                                                                             | Windows RS4 x86                                                                             | Ubuntu 16.04 x64                                                                            | Ubuntu 16.04 ARM64                                                                              |
| :-------- | :-----------------------------------------------------------------------------------------: | :-----------------------------------------------------------------------------------------: | :-----------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------: |
| Core 3.0  | [![full_opt_windows_RS4_x64_netcoreapp3.0_icon]][full_opt_windows_RS4_x64_netcoreapp3.0_status] | [![full_opt_windows_RS4_x86_netcoreapp3.0_icon]][full_opt_windows_RS4_x86_netcoreapp3.0_status] | [![full_opt_ubuntu_1604_x64_netcoreapp3.0_icon]][full_opt_ubuntu_1604_x64_netcoreapp3.0_status] | [![full_opt_ubuntu_1604_arm64_netcoreapp3.0_icon]][full_opt_ubuntu_1604_arm64_netcoreapp3.0_status] |
| Core 2.2  | [![full_opt_windows_RS4_x64_netcoreapp2.2_icon]][full_opt_windows_RS4_x64_netcoreapp2.2_status] | [![full_opt_windows_RS4_x86_netcoreapp2.2_icon]][full_opt_windows_RS4_x86_netcoreapp2.2_status] | [![full_opt_ubuntu_1604_x64_netcoreapp2.2_icon]][full_opt_ubuntu_1604_x64_netcoreapp2.2_status] | N/A                                                                                             |
| Core 2.1  | [![full_opt_windows_RS4_x64_netcoreapp2.1_icon]][full_opt_windows_RS4_x64_netcoreapp2.1_status] | [![full_opt_windows_RS4_x86_netcoreapp2.1_icon]][full_opt_windows_RS4_x86_netcoreapp2.1_status] | [![full_opt_ubuntu_1604_x64_netcoreapp2.1_icon]][full_opt_ubuntu_1604_x64_netcoreapp2.1_status] | N/A                                                                                             |
| Core 2.0  | [![full_opt_windows_RS4_x64_netcoreapp2.0_icon]][full_opt_windows_RS4_x64_netcoreapp2.0_status] | [![full_opt_windows_RS4_x86_netcoreapp2.0_icon]][full_opt_windows_RS4_x86_netcoreapp2.0_status] | [![full_opt_ubuntu_1604_x64_netcoreapp2.0_icon]][full_opt_ubuntu_1604_x64_netcoreapp2.0_status] | N/A                                                                                             |
| .NET      | [![full_opt_windows_RS4_x64_net461_icon]][full_opt_windows_RS4_x64_net461_status]               | [![full_opt_windows_RS4_x86_net461_icon]][full_opt_windows_RS4_x86_net461_status]               | N/A                                                                                         | N/A                                                                                             |


#### Tiered

| Framework | Windows RS4 x64                                                                               | Windows RS4 x86                                                                               | Ubuntu 16.04 x64                                                                              | Ubuntu 16.04 ARM64                                                                                |
| :-------- | :-------------------------------------------------------------------------------------------: | :-------------------------------------------------------------------------------------------: | :-------------------------------------------------------------------------------------------: | :-----------------------------------------------------------------------------------------------: |
| Core 3.0  | [![tiered_windows_RS4_x64_netcoreapp3.0_icon]][tiered_windows_RS4_x64_netcoreapp3.0_status] | [![tiered_windows_RS4_x86_netcoreapp3.0_icon]][tiered_windows_RS4_x86_netcoreapp3.0_status] | [![tiered_ubuntu_1604_x64_netcoreapp3.0_icon]][tiered_ubuntu_1604_x64_netcoreapp3.0_status] | [![tiered_ubuntu_1604_arm64_netcoreapp3.0_icon]][tiered_ubuntu_1604_arm64_netcoreapp3.0_status] |
| Core 2.2  | [![tiered_windows_RS4_x64_netcoreapp2.2_icon]][tiered_windows_RS4_x64_netcoreapp2.2_status] | [![tiered_windows_RS4_x86_netcoreapp2.2_icon]][tiered_windows_RS4_x86_netcoreapp2.2_status] | [![tiered_ubuntu_1604_x64_netcoreapp2.2_icon]][tiered_ubuntu_1604_x64_netcoreapp2.2_status] | N/A                                                                                               |
| Core 2.1  | [![tiered_windows_RS4_x64_netcoreapp2.1_icon]][tiered_windows_RS4_x64_netcoreapp2.1_status] | [![tiered_windows_RS4_x86_netcoreapp2.1_icon]][tiered_windows_RS4_x86_netcoreapp2.1_status] | [![tiered_ubuntu_1604_x64_netcoreapp2.1_icon]][tiered_ubuntu_1604_x64_netcoreapp2.1_status] | N/A                                                                                               |


[//]: # (These are the full_opt links)

[//]: # (These are the windows x64 links)
[full_opt_windows_RS4_x64_netcoreapp3.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp3.0
[full_opt_windows_RS4_x64_netcoreapp3.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp3.0
[full_opt_windows_RS4_x64_netcoreapp2.2_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp2.2
[full_opt_windows_RS4_x64_netcoreapp2.2_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp2.2
[full_opt_windows_RS4_x64_netcoreapp2.1_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp2.1
[full_opt_windows_RS4_x64_netcoreapp2.1_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp2.1
[full_opt_windows_RS4_x64_netcoreapp2.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp2.0
[full_opt_windows_RS4_x64_netcoreapp2.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_netcoreapp2.0
[full_opt_windows_RS4_x64_net461_status]:            https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_net461
[full_opt_windows_RS4_x64_net461_icon]:              https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=full_opt_net461

[//]: # (These are the windows x86 links)
[full_opt_windows_RS4_x86_netcoreapp3.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp3.0
[full_opt_windows_RS4_x86_netcoreapp3.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp3.0
[full_opt_windows_RS4_x86_netcoreapp2.2_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp2.2
[full_opt_windows_RS4_x86_netcoreapp2.2_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp2.2
[full_opt_windows_RS4_x86_netcoreapp2.1_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp2.1
[full_opt_windows_RS4_x86_netcoreapp2.1_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp2.1
[full_opt_windows_RS4_x86_netcoreapp2.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp2.0
[full_opt_windows_RS4_x86_netcoreapp2.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_netcoreapp2.0
[full_opt_windows_RS4_x86_net461_status]:            https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_net461
[full_opt_windows_RS4_x86_net461_icon]:              https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=full_opt_net461

[//]: # (These are the ubuntu x64 links)
[full_opt_ubuntu_1604_x64_netcoreapp3.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp3.0
[full_opt_ubuntu_1604_x64_netcoreapp3.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp3.0
[full_opt_ubuntu_1604_x64_netcoreapp2.2_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp2.2
[full_opt_ubuntu_1604_x64_netcoreapp2.2_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp2.2
[full_opt_ubuntu_1604_x64_netcoreapp2.1_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp2.1
[full_opt_ubuntu_1604_x64_netcoreapp2.1_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp2.1
[full_opt_ubuntu_1604_x64_netcoreapp2.0_status]:     https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp2.0
[full_opt_ubuntu_1604_x64_netcoreapp2.0_icon]:       https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64&configuration=full_opt_netcoreapp2.0

[//]: # (These are the ubuntu arm64 links)
[full_opt_ubuntu_1604_arm64_netcoreapp3.0_status]:   https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20arm64&configuration=full_opt_netcoreapp3.0
[full_opt_ubuntu_1604_arm64_netcoreapp3.0_icon]:     https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20arm64&configuration=full_opt_netcoreapp3.0

[//]: # (These are the tiered links)

[//]: # (These are the windows x64 links)
[tiered_windows_RS4_x64_netcoreapp3.0_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=tiered_netcoreapp3.0
[tiered_windows_RS4_x64_netcoreapp3.0_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=tiered_netcoreapp3.0
[tiered_windows_RS4_x64_netcoreapp2.2_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=tiered_netcoreapp2.2
[tiered_windows_RS4_x64_netcoreapp2.2_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=tiered_netcoreapp2.2
[tiered_windows_RS4_x64_netcoreapp2.1_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x64&configuration=tiered_netcoreapp2.1
[tiered_windows_RS4_x64_netcoreapp2.1_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x64&configuration=tiered_netcoreapp2.1

[//]: # (These are the windows x86 links)
[tiered_windows_RS4_x86_netcoreapp3.0_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=tiered_netcoreapp3.0
[tiered_windows_RS4_x86_netcoreapp3.0_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=tiered_netcoreapp3.0
[tiered_windows_RS4_x86_netcoreapp2.2_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=tiered_netcoreapp2.2
[tiered_windows_RS4_x86_netcoreapp2.2_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=tiered_netcoreapp2.2
[tiered_windows_RS4_x86_netcoreapp2.1_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=windows%20RS4%20x86&configuration=tiered_netcoreapp2.1
[tiered_windows_RS4_x86_netcoreapp2.1_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=windows%20RS4%20x86&configuration=tiered_netcoreapp2.1

[//]: # (These are the ubuntu x64 links)
[tiered_ubuntu_1604_x64_netcoreapp3.0_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64&configuration=tiered_netcoreapp3.0
[tiered_ubuntu_1604_x64_netcoreapp3.0_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64&configuration=tiered_netcoreapp3.0
[tiered_ubuntu_1604_x64_netcoreapp2.2_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64&configuration=tiered_netcoreapp2.2
[tiered_ubuntu_1604_x64_netcoreapp2.2_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64&configuration=tiered_netcoreapp2.2
[tiered_ubuntu_1604_x64_netcoreapp2.1_status]:    https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20x64&configuration=tiered_netcoreapp2.1
[tiered_ubuntu_1604_x64_netcoreapp2.1_icon]:      https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20x64&configuration=tiered_netcoreapp2.1

[//]: # (These are the ubuntu arm64 links)
[tiered_ubuntu_1604_arm64_netcoreapp3.0_status]:  https://dev.azure.com/dnceng/public/_build/latest?definitionId=271&branchName=master&jobName=ubuntu%201604%20arm64&configuration=tiered_netcoreapp3.0
[tiered_ubuntu_1604_arm64_netcoreapp3.0_icon]:    https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/performance/performance-ci?branchName=master&jobName=ubuntu%201604%20arm64&configuration=tiered_netcoreapp3.0