# .NET Core Performance CI Status

## [CoreCLR](https://github.com/dotnet/coreclr)

### Code Quality

[//]: # (https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_Ubuntu16.04/lastCompletedBuild/buildTimestamp)

#### Code Quality / Windows

| Branch        | arch=[x64]<br>OptLevel=[full]                      | arch=[x64]<br>OptLevel=[min]                     | arch=[x86]<br>OptLevel=[full]                      | arch=[x86]<br>OptLevel=[min]                     |
| :------------ | :------------------------------------------------: | :----------------------------------------------: | :------------------------------------------------: | :----------------------------------------------: |
| master        | [![master_x64_win_full_icon]][master_x64_win_full] | [![master_x64_win_min_icon]][master_x64_win_min] | [![master_x86_win_full_icon]][master_x86_win_full] | [![master_x86_win_min_icon]][master_x86_win_min] |
| release/2.2   | [![rel2.2_x64_win_full_icon]][rel2.2_x64_win_full] | N/A                                              | [![rel2.2_x86_win_full_icon]][rel2.2_x86_win_full] | N/A                                              |
| release/2.1   | [![rel2.1_x64_win_full_icon]][rel2.1_x64_win_full] | N/A                                              | [![rel2.1_x86_win_full_icon]][rel2.1_x86_win_full] | N/A                                              |
| release/2.0.0 | [![rel2.0_x64_win_full_icon]][rel2.0_x64_win_full] | N/A                                              | [![rel2.0_x86_win_full_icon]][rel2.0_x86_win_full] | N/A                                              |
| release/1.1.0 | [![rel1.1_x64_win_full_icon]][rel1.1_x64_win_full] | N/A                                              | [![rel1.1_x86_win_full_icon]][rel1.1_x86_win_full] | N/A                                              |

#### Code Quality / Ubuntu 16.04

| Branch        | arch=[x64]<br>OptLevel=[full]                      |
| :------------ | :------------------------------------------------: |
| master        | [![master_x64_nix_full_icon]][master_x64_nix_full] |
| release/2.2   | [![rel2.2_x64_nix_full_icon]][rel2.2_x64_nix_full] |
| release/2.1   | [![rel2.1_x64_nix_full_icon]][rel2.1_x64_nix_full] |
| release/2.0.0 | [![rel2.0_x64_nix_full_icon]][rel2.0_x64_nix_full] |
| release/1.1.0 | [![rel1.1_x64_nix_full_icon]][rel1.1_x64_nix_full] |

[//]: # (These are the x64 links)
[master_x64_nix_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_Ubuntu16.04_x64/lastCompletedBuild/
[master_x64_nix_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_Ubuntu16.04_x64/lastCompletedBuild/badge/icon (Run Status)
[master_x64_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[master_x64_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[master_x64_win_min]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[master_x64_win_min_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_x64_nix_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_Ubuntu16.04/lastCompletedBuild/
[rel2.2_x64_nix_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_x64_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.2_x64_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_x64_nix_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_Ubuntu16.04/lastCompletedBuild/
[rel2.1_x64_nix_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_x64_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.1_x64_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_x64_nix_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_Ubuntu16.04/lastCompletedBuild/
[rel2.0_x64_nix_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_x64_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/
[rel2.0_x64_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/badge/icon (Run Status)
[rel1.1_x64_nix_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_Ubuntu16.04/lastCompletedBuild/
[rel1.1_x64_nix_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_Ubuntu16.04/lastCompletedBuild/badge/icon (Run Status)
[rel1.1_x64_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/
[rel1.1_x64_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x64/lastCompletedBuild/badge/icon (Run Status)

[//]: # (These are the x86 links)
[master_x86_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[master_x86_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[master_x86_win_min]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[master_x86_win_min_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_perflab_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_x86_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[rel2.2_x86_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_x86_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[rel2.1_x86_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_perflab_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_x86_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/
[rel2.0_x86_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/badge/icon (Run Status)
[rel1.1_x86_win_full]:                  https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/
[rel1.1_x86_win_full_icon]:             https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_1.1.0/job/perf_perflab_Windows_NT_x86/lastCompletedBuild/badge/icon (Run Status)

### End-to-End

#### End-to-End / Windows

| Branch      | arch=[x64]<br>OptLevel=[full]                              | arch=[x64]<br>OptLevel=[min]                             | arch=[x64]<br>OptLevel=[tiered]                                | arch=[x86]<br>OptLevel=[full]                              | arch=[x86]<br>OptLevel=[min]                             | arch=[x86]<br>OptLevel=[tiered]                                |
| :---------- | :--------------------------------------------------------: | :------------------------------------------------------: | :------------------------------------------------------------: | :--------------------------------------------------------: | :------------------------------------------------------: | :------------------------------------------------------------: |
| master      | [![master_e2e_x64_win_full_icon]][master_e2e_x64_win_full] | [![master_e2e_x64_win_min_icon]][master_e2e_x64_win_min] | [![master_e2e_x64_win_tiered_icon]][master_e2e_x64_win_tiered] | [![master_e2e_x86_win_full_icon]][master_e2e_x86_win_full] | [![master_e2e_x86_win_min_icon]][master_e2e_x86_win_min] | [![master_e2e_x86_win_tiered_icon]][master_e2e_x86_win_tiered] |
| release/2.2 | [![rel2.2_e2e_x64_win_full_icon]][rel2.2_e2e_x64_win_full] | N/A                                                      | N/A                                                            | [![rel2.2_e2e_x86_win_full_icon]][rel2.2_e2e_x86_win_full] | N/A                                                      | N/A                                                            |
| release/2.1 | [![rel2.1_e2e_x64_win_full_icon]][rel2.1_e2e_x64_win_full] | N/A                                                      | N/A                                                            | [![rel2.1_e2e_x86_win_full_icon]][rel2.1_e2e_x86_win_full] | N/A                                                      | N/A                                                            |

[//]: # (These are the x64 links)
[master_e2e_x64_win_full]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[master_e2e_x64_win_full_icon]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[master_e2e_x64_win_min]:               https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/
[master_e2e_x64_win_min_icon]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_min_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[master_e2e_x64_win_tiered]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/
[master_e2e_x64_win_tiered_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x64_tiered_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_e2e_x64_win_full]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.2_e2e_x64_win_full_icon]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_e2e_x64_win_full]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.1_e2e_x64_win_full_icon]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)

[//]: # (These are the x86 links)
[master_e2e_x86_win_full]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[master_e2e_x86_win_full_icon]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[master_e2e_x86_win_min]:               https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/
[master_e2e_x86_win_min_icon]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_min_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[master_e2e_x86_win_tiered]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/
[master_e2e_x86_win_tiered_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_scenarios_Windows_NT_x86_tiered_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_e2e_x86_win_full]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[rel2.2_e2e_x86_win_full_icon]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_e2e_x86_win_full]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/
[rel2.1_e2e_x86_win_full_icon]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_scenarios_Windows_NT_x86_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)

### Throughput

#### Throughput / Windows / x64

| Branch        | OptLevel=[full]<br>PGO=[nopgo]                                       | OptLevel=[full]<br>PGO=[pgo]                                     | OptLevel=[min]<br>PGO=[nopgo]                                      | OptLevel=[min]<br>PGO=[pgo]                                    |
| :------------ | :------------------------------------------------------------------: | :--------------------------------------------------------------: | :----------------------------------------------------------------: | :------------------------------------------------------------: |
| master        | [![master_TP_x64_win_full_nopgo_icon]][master_TP_x64_win_full_nopgo] | [![master_TP_x64_win_full_pgo_icon]][master_TP_x64_win_full_pgo] | [![master_TP_x64_win_min_nopgo_icon]][master_TP_x64_win_min_nopgo] | [![master_TP_x64_win_min_pgo_icon]][master_TP_x64_win_min_pgo] |
| release/2.2   | [![rel2.2_TP_x64_win_full_nopgo_icon]][rel2.2_TP_x64_win_full_nopgo] | [![rel2.2_TP_x64_win_full_pgo_icon]][rel2.2_TP_x64_win_full_pgo] | N/A                                                                | N/A                                                            |
| release/2.1   | [![rel2.1_TP_x64_win_full_nopgo_icon]][rel2.1_TP_x64_win_full_nopgo] | [![rel2.1_TP_x64_win_full_pgo_icon]][rel2.1_TP_x64_win_full_pgo] | N/A                                                                | N/A                                                            |
| release/2.0.0 | N/A                                                                  | [![rel2.0_TP_x64_win_full_pgo_icon]][rel2.0_TP_x64_win_full_pgo] | N/A                                                                | [![rel2.0_TP_x64_win_min_pgo_icon]][rel2.0_TP_x64_win_min_pgo] |

#### Throughput / Ubuntu 16.04 / x64

| Branch        | OptLevel=[full]<br>PGO=[pgo]                                     | OptLevel=[min]<br>PGO=[pgo]                                    |
| :------------ | :--------------------------------------------------------------: | :------------------------------------------------------------: |
| master        | [![master_TP_x64_nix_full_pgo_icon]][master_TP_x64_nix_full_pgo] | [![master_TP_x64_nix_min_pgo_icon]][master_TP_x64_nix_min_pgo] |
| release/2.2   | [![rel2.2_TP_x64_nix_full_pgo_icon]][rel2.2_TP_x64_nix_full_pgo] | N/A                                                            |
| release/2.1   | [![rel2.1_TP_x64_nix_full_pgo_icon]][rel2.1_TP_x64_nix_full_pgo] | N/A                                                            |
| release/2.0.0 | [![rel2.0_TP_x64_nix_full_pgo_icon]][rel2.0_TP_x64_nix_full_pgo] | [![rel2.0_TP_x64_nix_min_pgo_icon]][rel2.0_TP_x64_nix_min_pgo] |

#### Throughput / Ubuntu 14.04 / arm

| Branch        | OptLevel=[full]<br>PGO=[pgo]                                     |  OptLevel=[min]<br>PGO=[pgo]                                    |
| :------------ | :--------------------------------------------------------------: |  :------------------------------------------------------------: |
| master        | [![master_TP_arm_nix_full_pgo_icon]][master_TP_arm_nix_full_pgo] |  [![master_TP_arm_nix_min_pgo_icon]][master_TP_arm_nix_min_pgo] |

[//]: # (These are the x64 links)
[master_TP_x64_nix_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu16.04_full_opt_x64/lastCompletedBuild/
[master_TP_x64_nix_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu16.04_full_opt_x64/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x64_nix_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu16.04_min_opt_x64/lastCompletedBuild/
[master_TP_x64_nix_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu16.04_min_opt_x64/lastCompletedBuild/badge/icon (Run Status)
[master_TP_arm_nix_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_full_opt_arm/lastCompletedBuild/
[master_TP_arm_nix_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_full_opt_arm/lastCompletedBuild/badge/icon (Run Status)
[master_TP_arm_nix_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_min_opt_arm/lastCompletedBuild/
[master_TP_arm_nix_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_Ubuntu14.04_min_opt_arm/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x64_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x64_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x64_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x64_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x64_win_min_nopgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x64_win_min_nopgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x64_win_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x64_win_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x64_min_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_TP_x64_nix_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/
[rel2.2_TP_x64_nix_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_TP_x64_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.2_TP_x64_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_TP_x64_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/
[rel2.2_TP_x64_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_TP_x64_nix_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/
[rel2.1_TP_x64_nix_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_TP_x64_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.1_TP_x64_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_TP_x64_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/
[rel2.1_TP_x64_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x64_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_TP_x64_nix_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/
[rel2.0_TP_x64_nix_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_full_opt/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_TP_x64_nix_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_min_opt/lastCompletedBuild/
[rel2.0_TP_x64_nix_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_Ubuntu16.04_min_opt/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_TP_x64_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_full_opt/lastCompletedBuild/
[rel2.0_TP_x64_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_full_opt/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_TP_x64_win_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_min_opt/lastCompletedBuild/
[rel2.0_TP_x64_win_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x64_min_opt/lastCompletedBuild/badge/icon (Run Status)

#### Throughput / Windows / x86

| Branch        | OptLevel=[full]<br>PGO=[nopgo]                                       | OptLevel=[full]<br>PGO=[pgo]                                     | OptLevel=[min]<br>PGO=[nopgo]                                      | OptLevel=[min]<br>PGO=[pgo]                                    |
| :------------ | :------------------------------------------------------------------: | :--------------------------------------------------------------: | :----------------------------------------------------------------: | :------------------------------------------------------------: |
| master        | [![master_TP_x86_win_full_nopgo_icon]][master_TP_x86_win_full_nopgo] | [![master_TP_x86_win_full_pgo_icon]][master_TP_x86_win_full_pgo] | [![master_TP_x86_win_min_nopgo_icon]][master_TP_x86_win_min_nopgo] | [![master_TP_x86_win_min_pgo_icon]][master_TP_x86_win_min_pgo] |
| release/2.2   | [![rel2.2_TP_x86_win_full_nopgo_icon]][rel2.2_TP_x86_win_full_nopgo] | [![rel2.2_TP_x86_win_full_pgo_icon]][rel2.2_TP_x86_win_full_pgo] | N/A                                                                | N/A                                                            |
| release/2.1   | [![rel2.1_TP_x86_win_full_nopgo_icon]][rel2.1_TP_x86_win_full_nopgo] | [![rel2.1_TP_x86_win_full_pgo_icon]][rel2.1_TP_x86_win_full_pgo] | N/A                                                                | N/A                                                            |
| release/2.0.0 | N/A                                                                  | [![rel2.0_TP_x86_win_full_pgo_icon]][rel2.0_TP_x86_win_full_pgo] | N/A                                                                | [![rel2.0_TP_x86_win_min_pgo_icon]][rel2.0_TP_x86_win_min_pgo] |

[//]: # (These are the x86 links)
[master_TP_x86_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x86_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x86_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x86_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x86_win_min_nopgo]:          https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_nopgo/lastCompletedBuild/
[master_TP_x86_win_min_nopgo_icon]:     https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[master_TP_x86_win_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_pgo/lastCompletedBuild/
[master_TP_x86_win_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_throughput_perflab_Windows_NT_x86_min_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_TP_x86_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.2_TP_x86_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_TP_x86_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/
[rel2.2_TP_x86_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_TP_x86_win_full_nopgo]:         https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/
[rel2.1_TP_x86_win_full_nopgo_icon]:    https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_nopgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_TP_x86_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/
[rel2.1_TP_x86_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_throughput_perflab_Windows_NT_x86_full_opt_ryujit_pgo/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_TP_x86_win_full_pgo]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_full_opt/lastCompletedBuild/
[rel2.0_TP_x86_win_full_pgo_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_full_opt/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_TP_x86_win_min_pgo]:            https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_min_opt/lastCompletedBuild/
[rel2.0_TP_x86_win_min_pgo_icon]:       https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.0.0/job/perf_throughput_perflab_Windows_NT_x86_min_opt/lastCompletedBuild/badge/icon (Run Status)

### IlLink / Windows

| Branch      | arch=[x64]<br>OptLevel=[full]                                    |
| :---------- | :--------------------------------------------------------------: |
| master      | [![master_illink_x64_win_full_icon]][master_illink_x64_win_full] |
| release/2.2 | [![rel2.2_illink_x64_win_full_icon]][rel2.2_illink_x64_win_full] |
| release/2.1 | [![rel2.1_illink_x64_win_full_icon]][rel2.1_illink_x64_win_full] |

[//]: # (These are the x64 links)
[master_illink_x64_win_full]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_illink_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[master_illink_x64_win_full_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/perf_illink_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_illink_x64_win_full]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_illink_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.2_illink_x64_win_full_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/perf_illink_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_illink_x64_win_full]:           https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_illink_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/
[rel2.1_illink_x64_win_full_icon]:      https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/perf_illink_Windows_NT_x64_full_opt_ryujit/lastCompletedBuild/badge/icon (Run Status)

### Size on disk / Windows

| Branch      | arch=[x64]                                       | arch=[x86]                                       |
| :---------- | :----------------------------------------------: | :----------------------------------------------: |
| master      | [![master_sod_x64_win_icon]][master_sod_x64_win] | [![master_sod_x86_win_icon]][master_sod_x86_win] |
| release/2.2 | [![rel2.2_sod_x64_win_icon]][rel2.2_sod_x64_win] | [![rel2.2_sod_x86_win_icon]][rel2.2_sod_x86_win] |
| release/2.1 | [![rel2.1_sod_x64_win_icon]][rel2.1_sod_x64_win] | [![rel2.1_sod_x86_win_icon]][rel2.1_sod_x86_win] |

[//]: # (These are the x64 links)
[master_sod_x64_win]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/sizeondisk_x64/lastCompletedBuild/
[master_sod_x64_win_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/sizeondisk_x64/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_sod_x64_win]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/sizeondisk_x64/lastCompletedBuild/
[rel2.2_sod_x64_win_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/sizeondisk_x64/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_sod_x64_win]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/sizeondisk_x64/lastCompletedBuild/
[rel2.1_sod_x64_win_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/sizeondisk_x64/lastCompletedBuild/badge/icon (Run Status)

[//]: # (These are the x86 links)
[master_sod_x86_win]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/sizeondisk_x86/lastCompletedBuild/
[master_sod_x86_win_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/master/job/sizeondisk_x86/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_sod_x86_win]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/sizeondisk_x86/lastCompletedBuild/
[rel2.2_sod_x86_win_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.2/job/sizeondisk_x86/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_sod_x86_win]:                   https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/sizeondisk_x86/lastCompletedBuild/
[rel2.1_sod_x86_win_icon]:              https://ci2.dot.net/job/dotnet_coreclr/job/perf/job/release_2.1/job/sizeondisk_x86/lastCompletedBuild/badge/icon (Run Status)

## [CoreFX](https://github.com/dotnet/corefx)

#### Code Quality / Windows

| Branch        | Configuration                                  |
| :------------ | :--------------------------------------------: |
| master        | [![master_corefx_win_icon]][master_corefx_win] |
| release/2.2   | [![rel2.2_corefx_win_icon]][rel2.2_corefx_win] |
| release/2.1   | [![rel2.1_corefx_win_icon]][rel2.1_corefx_win] |
| release/2.0.0 | [![rel2.0_corefx_win_icon]][rel2.0_corefx_win] |

#### Code Quality / Ubuntu 16.04

| Branch        | Configuration                                  |
| :------------ | :--------------------------------------------: |
| master        | [![master_corefx_nix_icon]][master_corefx_nix] |
| release/2.2   | [![rel2.2_corefx_nix_icon]][rel2.2_corefx_nix] |
| release/2.1   | [![rel2.1_corefx_nix_icon]][rel2.1_corefx_nix] |
| release/2.0.0 | [![rel2.0_corefx_nix_icon]][rel2.0_corefx_nix] |

[//]: # (These are the Windows_NT x64 links)
[master_corefx_win]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/master/job/perf_windows_nt_release/lastCompletedBuild/
[master_corefx_win_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/master/job/perf_windows_nt_release/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_corefx_win]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.0.0/job/perf_windows_nt_release/lastCompletedBuild/
[rel2.0_corefx_win_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.0.0/job/perf_windows_nt_release/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_corefx_win]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.1/job/perf_windows_nt_release/lastCompletedBuild/
[rel2.1_corefx_win_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.1/job/perf_windows_nt_release/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_corefx_win]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.2/job/perf_windows_nt_release/lastCompletedBuild/
[rel2.2_corefx_win_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.2/job/perf_windows_nt_release/lastCompletedBuild/badge/icon (Run Status)

[//]: # (These are the Ubuntu 16.04 x64 links)
[master_corefx_nix]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/master/job/perf_ubuntu16.04_release/lastCompletedBuild/
[master_corefx_nix_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/master/job/perf_ubuntu16.04_release/lastCompletedBuild/badge/icon (Run Status)
[rel2.0_corefx_nix]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.0.0/job/perf_ubuntu16.04_release/lastCompletedBuild/
[rel2.0_corefx_nix_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.0.0/job/perf_ubuntu16.04_release/lastCompletedBuild/badge/icon (Run Status)
[rel2.1_corefx_nix]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.1/job/perf_ubuntu16.04_release/lastCompletedBuild/
[rel2.1_corefx_nix_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.1/job/perf_ubuntu16.04_release/lastCompletedBuild/badge/icon (Run Status)
[rel2.2_corefx_nix]:                    https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.2/job/perf_ubuntu16.04_release/lastCompletedBuild/
[rel2.2_corefx_nix_icon]:               https://ci2.dot.net/job/dotnet_corefx/job/perf/job/release_2.2/job/perf_ubuntu16.04_release/lastCompletedBuild/badge/icon (Run Status)

## [Optimization](https://github.com/dotnet/optimization)

| Branch        | CoreCLR x86 Windows                              | CoreCLR x64 Windows                              | CoreCLR x64 Linux                                |
| :------------ | :----------------------------------------------: | :----------------------------------------------: | :----------------------------------------------: |
| master        | [![CLRx86WINmasPGO Status]][CLRx86WINmasPGO Url] | [![CLRx64WINmasPGO Status]][CLRx64WINmasPGO Url] | [![CLRx64LINmasPGO Status]][CLRx64LINmasPGO Url] |
| release/2.1   | [![CLRx86WIN2.1PGO Status]][CLRx86WIN2.1PGO Url] | [![CLRx64WIN2.1PGO Status]][CLRx64WIN2.1PGO Url] | [![CLRx64LIN2.1PGO Status]][CLRx64LIN2.1PGO Url] |
| release/2.0.0 | [![CLRx86WIN2.0PGO Status]][CLRx86WIN2.0PGO Url] | [![CLRx64WIN2.0PGO Status]][CLRx64WIN2.0PGO Url] | [![CLRx64LIN2.0PGO Status]][CLRx64LIN2.0PGO Url] |
| release/1.1.0 | N/A                                              | [![CLRx64WIN1.1PGO Status]][CLRx64WIN1.1PGO Url] | N/A                                              |

[CLRx86WINmasPGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx86WINmasPGO  (Run Status)
[CLRx64WINmasPGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx64WINmasPGO  (Run Status)
[CLRx64LINmasPGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx64LINmasPGO  (Run Status)
[CLRx86WIN2.1PGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx86WIN2.1PGO  (Run Status)
[CLRx64WIN2.1PGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx64WIN2.1PGO  (Run Status)
[CLRx64LIN2.1PGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx64LIN2.1PGO  (Run Status)
[CLRx86WIN2.0PGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx86WIN2.0PGO  (Run Status)
[CLRx64WIN2.0PGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx64WIN2.0PGO  (Run Status)
[CLRx64LIN2.0PGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx64LIN2.0PGO  (Run Status)
[CLRx64WIN1.1PGO Status]:   https://ci2.dot.net/buildStatus/icon?job=Private/dotnet_optimization/master/CLRx64WIN1.1PGO  (Run Status)

[CLRx86WINmasPGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx86WINmasPGO/
[CLRx64WINmasPGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx64WINmasPGO/
[CLRx64LINmasPGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx64LINmasPGO/
[CLRx86WIN2.1PGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx86WIN2.1PGO/
[CLRx64WIN2.1PGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx64WIN2.1PGO/
[CLRx64LIN2.1PGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx64LIN2.1PGO/
[CLRx86WIN2.0PGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx86WIN2.0PGO/
[CLRx64WIN2.0PGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx64WIN2.0PGO/
[CLRx64LIN2.0PGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx64LIN2.0PGO/
[CLRx64WIN1.1PGO Url]:      https://ci2.dot.net/job/Private/job/dotnet_optimization/job/master/job/CLRx64WIN1.1PGO/
