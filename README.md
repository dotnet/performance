# .NET Performance

| Public Build Status                         | Internal Build Status                           |
| :------------------------------------------ | :---------------------------------------------: |
| [![public_build_icon]][public_build_status] | [![internal_build_icon]][internal_build_status] |

This repo contains benchmarks used for testing the performance of all .NET Runtimes: .NET Core, Full .NET Framework, Mono and CoreRT.

Finding these benchmarks in a separate repository might be surprising. Performance in a given scenario may be impacted by changes in seemingly unrelated components. Using this central repository ensures that measurements are made in comparable ways across all .NET runtimes and repos. This consistency lets engineers make progress and ensures the customer scenarios are protected.

## Documentation

* [Microbenchmarks Guide](./src/benchmarks/micro/README.md) for information on running our microbenchmarks
* [Real-World Scenarios Guide](./src/benchmarks/real-world/JitBench/README.md) for information on running our real-world scenario benchmarks
* [Benchmarking workflow for dotnet/runtime repository](./docs/benchmarking-workflow-dotnet-runtime.md) for information on benchmarking local [dotnet/runtime](https://github.com/dotnet/runtime) builds
* [Profiling workflow for dotnet/runtime repository](./docs/profiling-workflow-dotnet-runtime.md) for information on profiling local [dotnet/runtime](https://github.com/dotnet/runtime) builds

## Contributing to Repository

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).