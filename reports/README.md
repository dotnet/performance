# .NET 9 to .NET 10 Performance Regression Reports

This directory contains reports detailing performance regressions that occurred between .NET 9 and .NET 10. Below is a list of the available reports. Please note that some linked commits may not be the exact commit reponsible, but were a best guess at the likely commit given the range of commits that the regression occurred in.

- [CoreCLR x64 Windows](./windows-x64-tiger_changepoint_report.md)
- [CoreCLR x64 Linux](./linux-x64-tiger_changepoint_report.md)
- [CoreCLR Arm64 Linux](./linux-arm64-ampere_changepoint_report.md)
- [Mono x64 Linux](./linux-mono-tiger_changepoint_report.md)
- [MonoAOT Arm64 Linux](./linux-monoaot-ampere_changepoint_report.md)
- [WASM x64 Linux](./linux-wasm-tiger_changepoint_report.md)