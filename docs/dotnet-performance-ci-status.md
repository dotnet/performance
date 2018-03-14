# .NET Core Performance CI Status

## [CoreCLR](https://github.com/dotnet/coreclr)

### Code Quality / Release / PGO / Ryujit

| Branch        | OS=[Ubuntu16.04]<br>arch=[x64]<br>OptLevel=[full]                        | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[full]                         | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[min]                        | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[full]                         | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[min]                        |
| :------------ | :----------------------------------------------------------------------: | :----------------------------------------------------------------------: | :--------------------------------------------------------------------: | :----------------------------------------------------------------------: | :--------------------------------------------------------------------: |
| master        | [![Run Status][master_x64_nix_full_icon]][master_x64_nix_full]           | [![Run Status][master_x64_win_full_icon]][master_x64_win_full]           | [![Run Status][master_x64_win_min_icon]][master_x64_win_min]           | [![Run Status][master_x86_win_full_icon]][master_x86_win_full]           | [![Run Status][master_x86_win_min_icon]][master_x86_win_min]           |
| release/2.1   | [![Run Status][release/2.1_x64_nix_full_icon]][release/2.1_x64_nix_full] | [![Run Status][release/2.1_x64_win_full_icon]][release/2.1_x64_win_full] | [![Run Status][release/2.1_x64_win_min_icon]][release/2.1_x64_win_min] | [![Run Status][release/2.1_x86_win_full_icon]][release/2.1_x86_win_full] | [![Run Status][release/2.1_x86_win_min_icon]][release/2.1_x86_win_min] |
| release/2.0.0 | [![Run Status][release/2.0_x64_nix_full_icon]][release/2.0_x64_nix_full] | [![Run Status][release/2.0_x64_win_full_icon]][release/2.0_x64_win_full] | N/A                                                                    | [![Run Status][release/2.0_x86_win_full_icon]][release/2.0_x86_win_full] | N/A                                                                    |
| release/1.1.0 | [![Run Status][release/1.1_x64_nix_full_icon]][release/1.1_x64_nix_full] | [![Run Status][release/1.1_x64_win_full_icon]][release/1.1_x64_win_full] | N/A                                                                    | [![Run Status][release/1.1_x86_win_full_icon]][release/1.1_x86_win_full] | N/A                                                                    |

[//]: # (These are the x64 links)
[master_x64_nix_full]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_Ubuntu16.04/lastCompletedBuild/
[master_x64_nix_full_icon]: https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[master_x64_win_full]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[master_x64_win_full_icon]: https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_x64_win_min]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[master_x64_win_min_icon]:  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon

[release/2.1_x64_nix_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_Ubuntu16.04/lastCompletedBuild/
[release/2.1_x64_nix_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[release/2.1_x64_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[release/2.1_x64_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[release/2.1_x64_win_min]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[release/2.1_x64_win_min_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon

[release/2.0_x64_nix_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_Ubuntu16.04/lastCompletedBuild/
[release/2.0_x64_nix_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[release/2.0_x64_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/
[release/2.0_x64_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/badge/icon

[release/1.1_x64_nix_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_Ubuntu16.04/lastCompletedBuild/
[release/1.1_x64_nix_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[release/1.1_x64_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/
[release/1.1_x64_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/badge/icon

[//]: # (These are the x86 links)
[master_x86_win_full]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[master_x86_win_full_icon]: https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_x86_win_min]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[master_x86_win_min_icon]:  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon

[release/2.1_x86_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[release/2.1_x86_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[release/2.1_x86_win_min]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[release/2.1_x86_win_min_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon

[release/2.0_x86_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/
[release/2.0_x86_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/badge/icon

[release/1.1_x86_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/
[release/1.1_x86_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/badge/icon

### Real World Code / Release / PGO / Ryujit

| Branch      | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[full]                                           | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[min]                                          | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[tiered]                                             | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[full]                                           | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[min]                                          | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[tiered]                                             |
| :---------- | :----------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------: | :----------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------: | :--------------------------------------------------------------------------------------------: |
| master      | [![Run Status][master_scenario_x64_win_full_icon]][master_scenario_x64_win_full]           | [![Run Status][master_scenario_x64_win_min_icon]][master_scenario_x64_win_min]           | [![Run Status][master_scenario_x64_win_tiered_icon]][master_scenario_x64_win_tiered]           | [![Run Status][master_scenario_x86_win_full_icon]][master_scenario_x86_win_full]           | [![Run Status][master_scenario_x86_win_min_icon]][master_scenario_x86_win_min]           | [![Run Status][master_scenario_x86_win_tiered_icon]][master_scenario_x86_win_tiered]           |
| release/2.1 | [![Run Status][release/2.1_scenario_x64_win_full_icon]][release/2.1_scenario_x64_win_full] | [![Run Status][release/2.1_scenario_x64_win_min_icon]][release/2.1_scenario_x64_win_min] | [![Run Status][release/2.1_scenario_x64_win_tiered_icon]][release/2.1_scenario_x64_win_tiered] | [![Run Status][release/2.1_scenario_x86_win_full_icon]][release/2.1_scenario_x86_win_full] | [![Run Status][release/2.1_scenario_x86_win_min_icon]][release/2.1_scenario_x86_win_min] | [![Run Status][release/2.1_scenario_x86_win_tiered_icon]][release/2.1_scenario_x86_win_tiered] |

[//]: # (These are the x64 links)
[master_scenario_x64_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[master_scenario_x64_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x64_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[master_scenario_x64_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x64_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/
[master_scenario_x64_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/badge/icon

[release/2.1_scenario_x64_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[release/2.1_scenario_x64_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[release/2.1_scenario_x64_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[release/2.1_scenario_x64_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon
[release/2.1_scenario_x64_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/
[release/2.1_scenario_x64_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/badge/icon

[//]: # (These are the x86 links)
[master_scenario_x86_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[master_scenario_x86_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x86_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[master_scenario_x86_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x86_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/
[master_scenario_x86_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/badge/icon

[release/2.1_scenario_x86_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[release/2.1_scenario_x86_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[release/2.1_scenario_x86_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[release/2.1_scenario_x86_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon
[release/2.1_scenario_x86_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/
[release/2.1_scenario_x86_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/badge/icon
