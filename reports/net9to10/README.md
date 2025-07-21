# .NET 9 to .NET 10 Performance Reports

This directory contains reports detailing performance changes that occurred between .NET 9 and .NET 10. Below is a list of the available reports. Please note we only have linked the regressions to the commits that caused them, and improvements are unlinked and may contain noise. Some linked commits may not be the exact commit reponsible, but were a best guess at the likely commit given the range of commits that the regression occurred in.

- CoreCLR x64 Windows: ([Regressions](./windows-x64-tiger_regression_report.md), [Improvements](./windows-x64-tiger_improvement_report.md))
- CoreCLR x64 Linux: ([Regressions](./linux-x64-tiger_regression_report.md), [Improvements](./linux-x64-tiger_improvement_report.md))
- CoreCLR Arm64 Linux: ([Regressions](./linux-arm64-ampere_regression_report.md), [Improvements](./linux-arm64-ampere_improvement_report.md))
- Mono x64 Linux: ([Regressions](./linux-mono-tiger_regression_report.md), [Improvements](./linux-mono-tiger_improvement_report.md))
- MonoAOT Arm64 Linux: ([Regressions](./linux-monoaot-ampere_regression_report.md), [Improvements](./linux-monoaot-ampere_improvement_report.md))
- WASM x64 Linux: ([Regressions](./linux-wasm-tiger_regression_report.md), [Improvements](./linux-wasm-tiger_improvement_report.md))