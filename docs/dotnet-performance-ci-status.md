# .NET Core Performance CI Status

## [CoreCLR](https://github.com/dotnet/coreclr)

### Code Quality / Release / PGO / Ryujit

#### Code Quality / Release / PGO / Ryujit / Windows_NT

| Branch        | arch=[x64]<br>OptLevel=[full]                                  | arch=[x64]<br>OptLevel=[min]                                 | arch=[x86]<br>OptLevel=[full]                                  | arch=[x86]<br>OptLevel=[min]                                 |
| :------------ | :------------------------------------------------------------: | :----------------------------------------------------------: | :------------------------------------------------------------: | :----------------------------------------------------------: |
| master        | [![Run Status][master_x64_win_full_icon]][master_x64_win_full] | [![Run Status][master_x64_win_min_icon]][master_x64_win_min] | [![Run Status][master_x86_win_full_icon]][master_x86_win_full] | [![Run Status][master_x86_win_min_icon]][master_x86_win_min] |
| release/2.1   | [![Run Status][rel2.1_x64_win_full_icon]][rel2.1_x64_win_full] | [![Run Status][rel2.1_x64_win_min_icon]][rel2.1_x64_win_min] | [![Run Status][rel2.1_x86_win_full_icon]][rel2.1_x86_win_full] | [![Run Status][rel2.1_x86_win_min_icon]][rel2.1_x86_win_min] |
| release/2.0.0 | [![Run Status][rel2.0_x64_win_full_icon]][rel2.0_x64_win_full] | N/A                                                          | [![Run Status][rel2.0_x86_win_full_icon]][rel2.0_x86_win_full] | N/A                                                          |
| release/1.1.0 | [![Run Status][rel1.1_x64_win_full_icon]][rel1.1_x64_win_full] | N/A                                                          | [![Run Status][rel1.1_x86_win_full_icon]][rel1.1_x86_win_full] | N/A                                                          |

#### Code Quality / Release / PGO / Ryujit / Ubuntu 16.04

| Branch        | arch=[x64]<br>OptLevel=[full]                                  |
| :------------ | :------------------------------------------------------------: |
| master        | [![Run Status][master_x64_nix_full_icon]][master_x64_nix_full] |
| release/2.1   | [![Run Status][rel2.1_x64_nix_full_icon]][rel2.1_x64_nix_full] |
| release/2.0.0 | [![Run Status][rel2.0_x64_nix_full_icon]][rel2.0_x64_nix_full] |
| release/1.1.0 | [![Run Status][rel1.1_x64_nix_full_icon]][rel1.1_x64_nix_full] |

[//]: # (These are the x64 links)
[master_x64_nix_full]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_Ubuntu16.04/lastCompletedBuild/
[master_x64_nix_full_icon]: https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[master_x64_win_full]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[master_x64_win_full_icon]: https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_x64_win_min]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[master_x64_win_min_icon]:  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon

[rel2.1_x64_nix_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_Ubuntu16.04/lastCompletedBuild/
[rel2.1_x64_nix_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[rel2.1_x64_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.1_x64_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[rel2.1_x64_win_min]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[rel2.1_x64_win_min_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon

[rel2.0_x64_nix_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_Ubuntu16.04/lastCompletedBuild/
[rel2.0_x64_nix_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[rel2.0_x64_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/
[rel2.0_x64_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/badge/icon

[rel1.1_x64_nix_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_Ubuntu16.04/lastCompletedBuild/
[rel1.1_x64_nix_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon
[rel1.1_x64_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/
[rel1.1_x64_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/badge/icon

[//]: # (These are the x86 links)
[master_x86_win_full]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[master_x86_win_full_icon]: https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_x86_win_min]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[master_x86_win_min_icon]:  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon

[rel2.1_x86_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[rel2.1_x86_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[rel2.1_x86_win_min]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[rel2.1_x86_win_min_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon

[rel2.0_x86_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/
[rel2.0_x86_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/badge/icon

[rel1.1_x86_win_full]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/
[rel1.1_x86_win_full_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/badge/icon

### Real World Code / Release / PGO / Ryujit

#### Real World Code / Release / PGO / Ryujit / Windows_NT

| Branch      | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[full]                                 | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[min]                                | OS=[Windows_NT]<br>arch=[x64]<br>OptLevel=[tiered]                                   | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[full]                                 | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[min]                                | OS=[Windows_NT]<br>arch=[x86]<br>OptLevel=[tiered]                                   |
| :---------- | :------------------------------------------------------------------------------: | :----------------------------------------------------------------------------: | :----------------------------------------------------------------------------------: | :------------------------------------------------------------------------------: | :----------------------------------------------------------------------------: | :----------------------------------------------------------------------------------: |
| master      | [![Run Status][master_scenario_x64_win_full_icon]][master_scenario_x64_win_full] | [![Run Status][master_scenario_x64_win_min_icon]][master_scenario_x64_win_min] | [![Run Status][master_scenario_x64_win_tiered_icon]][master_scenario_x64_win_tiered] | [![Run Status][master_scenario_x86_win_full_icon]][master_scenario_x86_win_full] | [![Run Status][master_scenario_x86_win_min_icon]][master_scenario_x86_win_min] | [![Run Status][master_scenario_x86_win_tiered_icon]][master_scenario_x86_win_tiered] |
| release/2.1 | [![Run Status][rel2.1_scenario_x64_win_full_icon]][rel2.1_scenario_x64_win_full] | [![Run Status][rel2.1_scenario_x64_win_min_icon]][rel2.1_scenario_x64_win_min] | [![Run Status][rel2.1_scenario_x64_win_tiered_icon]][rel2.1_scenario_x64_win_tiered] | [![Run Status][rel2.1_scenario_x86_win_full_icon]][rel2.1_scenario_x86_win_full] | [![Run Status][rel2.1_scenario_x86_win_min_icon]][rel2.1_scenario_x86_win_min] | [![Run Status][rel2.1_scenario_x86_win_tiered_icon]][rel2.1_scenario_x86_win_tiered] |

[//]: # (These are the x64 links)
[master_scenario_x64_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[master_scenario_x64_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x64_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[master_scenario_x64_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x64_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/
[master_scenario_x64_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/badge/icon

[rel2.1_scenario_x64_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.1_scenario_x64_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon
[rel2.1_scenario_x64_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[rel2.1_scenario_x64_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon
[rel2.1_scenario_x64_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/
[rel2.1_scenario_x64_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/badge/icon

[//]: # (These are the x86 links)
[master_scenario_x86_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[master_scenario_x86_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x86_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[master_scenario_x86_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon
[master_scenario_x86_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/
[master_scenario_x86_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/badge/icon

[rel2.1_scenario_x86_win_full]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[rel2.1_scenario_x86_win_full_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon
[rel2.1_scenario_x86_win_min]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[rel2.1_scenario_x86_win_min_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon
[rel2.1_scenario_x86_win_tiered]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/
[rel2.1_scenario_x86_win_tiered_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/badge/icon

### Throughput / Release / Ryujit

#### Throughput / Release / Ryujit / Windows_NT / x64

| Branch        | OptLevel=[full]<br>PGO=[nopgo]                                                   | OptLevel=[full]<br>PGO=[pgo]                                                 | OptLevel=[min]<br>PGO=[nopgo]                                                  | OptLevel=[min]<br>PGO=[pgo]                                                |
| :------------ | :------------------------------------------------------------------------------: | :--------------------------------------------------------------------------: | :----------------------------------------------------------------------------: | :------------------------------------------------------------------------: |
| master        | [![Run Status][master_TP_x64_win_full_nopgo_icon]][master_TP_x64_win_full_nopgo] | [![Run Status][master_TP_x64_win_full_pgo_icon]][master_TP_x64_win_full_pgo] | [![Run Status][master_TP_x64_win_min_nopgo_icon]][master_TP_x64_win_min_nopgo] | [![Run Status][master_TP_x64_win_min_pgo_icon]][master_TP_x64_win_min_pgo] |
| release/2.1   | [![Run Status][rel2.1_TP_x64_win_full_nopgo_icon]][rel2.1_TP_x64_win_full_nopgo] | [![Run Status][rel2.1_TP_x64_win_full_pgo_icon]][rel2.1_TP_x64_win_full_pgo] | [![Run Status][rel2.1_TP_x64_win_min_nopgo_icon]][rel2.1_TP_x64_win_min_nopgo] | [![Run Status][rel2.1_TP_x64_win_min_pgo_icon]][rel2.1_TP_x64_win_min_pgo] |
| release/2.0.0 | N/A                                                                              | [![Run Status][rel2.0_TP_x64_win_full_pgo_icon]][rel2.0_TP_x64_win_full_pgo] | N/A                                                                            | [![Run Status][rel2.0_TP_x64_win_min_pgo_icon]][rel2.0_TP_x64_win_min_pgo] |

#### Throughput / Release / Ryujit / Ubuntu 16.04 / x64

| Branch        | OptLevel=[full]<br>PGO=[nopgo] | OptLevel=[full]<br>PGO=[pgo]                                                 | OptLevel=[min]<br>PGO=[nopgo] | OptLevel=[min]<br>PGO=[pgo]                                                |
| :------------ | :----------------------------: | :--------------------------------------------------------------------------: | :---------------------------: | :------------------------------------------------------------------------: |
| master        | N/A                            | [![Run Status][master_TP_x64_nix_full_pgo_icon]][master_TP_x64_nix_full_pgo] | N/A                           | [![Run Status][master_TP_x64_nix_min_pgo_icon]][master_TP_x64_nix_min_pgo] |
| release/2.1   | N/A                            | [![Run Status][rel2.1_TP_x64_nix_full_pgo_icon]][rel2.1_TP_x64_nix_full_pgo] | N/A                           | [![Run Status][rel2.1_TP_x64_nix_min_pgo_icon]][rel2.1_TP_x64_nix_min_pgo] |
| release/2.0.0 | N/A                            | [![Run Status][rel2.0_TP_x64_nix_full_pgo_icon]][rel2.0_TP_x64_nix_full_pgo] | N/A                           | [![Run Status][rel2.0_TP_x64_nix_min_pgo_icon]][rel2.0_TP_x64_nix_min_pgo] |

[//]: # (These are the x64 links)
[master_TP_x64_nix_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_full_opt/lastCompletedBuild/
[master_TP_x64_nix_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_full_opt/lastCompletedBuild/badge/icon
[master_TP_x64_nix_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_min_opt/lastCompletedBuild/
[master_TP_x64_nix_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_min_opt/lastCompletedBuild/badge/icon
[master_TP_x64_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x64_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[master_TP_x64_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x64_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon
[master_TP_x64_win_min_nopgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x64_win_min_nopgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[master_TP_x64_win_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x64_win_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_pgo/lastCompletedBuild/badge/icon

[rel2.1_TP_x64_nix_full_pgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_Ubuntu14.04_full_opt/lastCompletedBuild/
[rel2.1_TP_x64_nix_full_pgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_Ubuntu14.04_full_opt/lastCompletedBuild/badge/icon
[rel2.1_TP_x64_nix_min_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_Ubuntu14.04_min_opt/lastCompletedBuild/
[rel2.1_TP_x64_nix_min_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_Ubuntu14.04_min_opt/lastCompletedBuild/badge/icon
[rel2.1_TP_x64_win_full_nopgo]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.1_TP_x64_win_full_nopgo_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[rel2.1_TP_x64_win_full_pgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/
[rel2.1_TP_x64_win_full_pgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon
[rel2.1_TP_x64_win_min_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.1_TP_x64_win_min_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[rel2.1_TP_x64_win_min_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_pgo/lastCompletedBuild/
[rel2.1_TP_x64_win_min_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_pgo/lastCompletedBuild/badge/icon

[rel2.0_TP_x64_nix_full_pgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/
[rel2.0_TP_x64_nix_full_pgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/badge/icon
[rel2.0_TP_x64_nix_min_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_min_opt/lastCompletedBuild/
[rel2.0_TP_x64_nix_min_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_min_opt/lastCompletedBuild/badge/icon
[rel2.0_TP_x64_win_full_pgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_full_opt/lastCompletedBuild/
[rel2.0_TP_x64_win_full_pgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_full_opt/lastCompletedBuild/badge/icon
[rel2.0_TP_x64_win_min_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_min_opt/lastCompletedBuild/
[rel2.0_TP_x64_win_min_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_min_opt/lastCompletedBuild/badge/icon

#### Throughput / Release / Ryujit / Windows_NT / x86

| Branch        | OptLevel=[full]<br>PGO=[nopgo]                                                   | OptLevel=[full]<br>PGO=[pgo]                                                 | OptLevel=[min]<br>PGO=[nopgo]                                                  | OptLevel=[min]<br>PGO=[pgo]                                                |
| :------------ | :------------------------------------------------------------------------------: | :--------------------------------------------------------------------------: | :----------------------------------------------------------------------------: | :------------------------------------------------------------------------: |
| master        | [![Run Status][master_TP_x86_win_full_nopgo_icon]][master_TP_x86_win_full_nopgo] | [![Run Status][master_TP_x86_win_full_pgo_icon]][master_TP_x86_win_full_pgo] | [![Run Status][master_TP_x86_win_min_nopgo_icon]][master_TP_x86_win_min_nopgo] | [![Run Status][master_TP_x86_win_min_pgo_icon]][master_TP_x86_win_min_pgo] |
| release/2.1   | [![Run Status][rel2.1_TP_x86_win_full_nopgo_icon]][rel2.1_TP_x86_win_full_nopgo] | [![Run Status][rel2.1_TP_x86_win_full_pgo_icon]][rel2.1_TP_x86_win_full_pgo] | [![Run Status][rel2.1_TP_x86_win_min_nopgo_icon]][rel2.1_TP_x86_win_min_nopgo] | [![Run Status][rel2.1_TP_x86_win_min_pgo_icon]][rel2.1_TP_x86_win_min_pgo] |
| release/2.0.0 | N/A                                                                              | [![Run Status][rel2.0_TP_x86_win_full_pgo_icon]][rel2.0_TP_x86_win_full_pgo] | N/A                                                                            | [![Run Status][rel2.0_TP_x86_win_min_pgo_icon]][rel2.0_TP_x86_win_min_pgo] |

[//]: # (These are the x86 links)
[master_TP_x86_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x86_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[master_TP_x86_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x86_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon
[master_TP_x86_win_min_nopgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x86_win_min_nopgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[master_TP_x86_win_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x86_win_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_pgo/lastCompletedBuild/badge/icon

[rel2.1_TP_x86_win_full_nopgo]:        https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.1_TP_x86_win_full_nopgo_icon]:   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[rel2.1_TP_x86_win_full_pgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/
[rel2.1_TP_x86_win_full_pgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon
[rel2.1_TP_x86_win_min_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.1_TP_x86_win_min_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_nopgo/lastCompletedBuild/badge/icon
[rel2.1_TP_x86_win_min_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_pgo/lastCompletedBuild/
[rel2.1_TP_x86_win_min_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_pgo/lastCompletedBuild/badge/icon

[rel2.0_TP_x86_win_full_pgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_full_opt/lastCompletedBuild/
[rel2.0_TP_x86_win_full_pgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_full_opt/lastCompletedBuild/badge/icon
[rel2.0_TP_x86_win_min_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_min_opt/lastCompletedBuild/
[rel2.0_TP_x86_win_min_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_min_opt/lastCompletedBuild/badge/icon

