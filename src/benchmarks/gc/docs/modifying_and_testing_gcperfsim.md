# Modifying and Testing GCPerfSim

When one makes modifications to _GCPerfSim_, it is of utmost importance to run
benchmarks to ensure the new changes don't break any other components or even
introduce regressions, which are later harder to find and fix.

This document covers the basics on how to perform these quality tests and do
basic comparisons between _GCPerfSim_ executables.

## Building and Setting Up

The first step is to do the changes you wish to _GCPerfSim_. Once that is done,
first make sure to back up its default build somewhere else.

For the purpose of this example, we will be using the following paths:

- **Performance Repo**: `C:\repos\performance`
- **Safe Backup Location**: `C:\repos\gcperfsim-backup`
- **Core Root Location**: `C:\repos\core_root`

Now that's established, make sure to copy the default _GCPerfSim_ build to the
safe location BEFORE building it again with your changes. In our example, this
would mean:

Copy `C:\repos\performance\artifacts\bin\GCPerfSim` to `C:\repos\gcperfsim-backup\GCPerfSim`.

Once that's done, you can build your new _GCPerfSim_.
In `C:\repos\performance\src\benchmarks\gc\src\exec\GCPerfSim` run the command:

```powershell
dotnet build -c release
```

Now, we have to tell the benchmark _yaml_ file to run using both _GCPerfSim_ dll's.

To ensure software quality remains, one ought to run the entire suite using both
_GCPerfSim_ builds. However, to keep this example simple, we will only be running
a simple variation of the _normal\_server_ test this time.

Adding the new _GCPerfSim_ build, the `yaml` file would look like this:

```yml
vary: executable
test_executables:
  orig_gcperfsim: C:\repos\gcperfsim-backup\GCPerfSim\release\netcoreapp5.0\GCPerfSim.dll
  mod_gcperfsim: C:\repos\performance\artifacts\bin\GCPerfSim\release\netcoreapp5.0\GCPerfSim.dll
coreclrs:
  a:
    core_root: C:\repos\core_root
options:
  default_iteration_count: 1
  default_max_seconds: 300
common_config:
  complus_gcserver: true
  complus_gcconcurrent: true
  complus_gcheapcount: 6
benchmarks:
  2gb:
    arguments:
      tc: 6
      tagb: 300
      tlgb: 2
      lohar: 0
      sohsi: 50
      lohsi: 0
      pohsi: 0
      sohpi: 0
      lohpi: 0
      pohpi: 0
      sohfi: 0
      lohfi: 0
      pohfi: 0
      allocType: reference
      testKind: time
scores:
  speed:
    FirstToLastGCSeconds:
      weight: 1
    PauseDurationMSec_95P:
      weight: 1

```

We are naming the original _GCPerfSim_ as `orig_gcperfsim`, and the modified
one as `mod_gcperfsim` in our bench file. For simplicity of this explanation,
we are only running the **2gb** benchmark. However, it is important you run at
least all the default benchmarks (0gb, 2gb, 2gb-pinning, 20gb) once, since
failure in one of them does not necessarily mean the others are affected as well.

Also, in this example we are only using one `Core_Root`. If you wish, you can
test with different ones although that's not strictly necessary.

Once this is ready, you can proceed to run the tests.
In `C:\repos\performance\src\benchmarks\gc`, run the following command:

```powershell
py . run bench\suite\normal_server.yaml
```

## Comparing Results

We've run our tests by now and everything seemingly went fine. No failures
happened whatsoever. If something went wrong when running them, then you've
found a problem you need to fix in your changes before any more development.

Let's check out some numbers to see if we encounter any abnormalities to
investigate. Run the following command:

```powershell
py . diff bench\suite\normal_server.yaml
```

This results in an output like the following one:

```text
Diff of base = orig_gcperfsim and new = mod_gcperfsim

                          ┌────────────────────────────┐
                          │ Summary of important stats │
                          └────────────────────────────┘


                                       │    PctTimePausedInGC │ FirstToLastGCSeconds
                                  name │ Base │  New │ % Diff │ Base │  New │ % Diff
  ─────────────────────────────────────┼──────┼──────┼────────┼──────┼──────┼───────
  DESKTOP-S0L8UMK__a__only_config__2gb │ 33.0 │ 30.7 │  -6.96 │ 31.6 │ 30.2 │  -4.46



                                       │ HeapSizeBeforeMB_Mean │ HeapSizeAfterMB_Mean
                                  name │ Base │  New │  % Diff │ Base │  New │ % Diff
  ─────────────────────────────────────┼──────┼──────┼─────────┼──────┼──────┼───────
  DESKTOP-S0L8UMK__a__only_config__2gb │ 5282 │ 5277 │ -0.0881 │ 4070 │ 4060 │ -0.233



                                       │ PauseDurationMSec_95PWhereIsGen0 │ PauseDurationMSec_95PWhereIsGen1
                                  name │ Base │  New │             % Diff │ Base │  New │             % Diff
  ─────────────────────────────────────┼──────┼──────┼────────────────────┼──────┼──────┼───────────────────
  DESKTOP-S0L8UMK__a__only_config__2gb │ 45.3 │ 41.6 │              -8.23 │  100 │ 95.9 │              -4.54



                                       │ PauseDurationMSec_95PWhereIsBackground │ PauseDurationMSec_95PWhereIsBlockingGen2
                                  name │ Base │  New │                   % Diff │        Base │         New │       % Diff
  ─────────────────────────────────────┼──────┼──────┼──────────────────────────┼─────────────┼─────────────┼─────────────
  DESKTOP-S0L8UMK__a__only_config__2gb │ 40.2 │ 36.4 │                    -9.62 │ <no values> │ <no values> │



                                 ┌──────────────────────────────────────┐
                                 │ DESKTOP-S0L8UMK__a__only_config__2gb │
                                 └──────────────────────────────────────┘


Improvements (Improvement of 5-20%)

                                          Metric │      Base (run 0) │       New (run 0) │  % Diff │ Abs Diff
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  CountIsBackground                              │                13 │                12 │   -7.69 │       -1
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  FirstEventToFirstGCSeconds                     │             0.655 │             0.615 │   -6.08 │  -0.0398
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationMSec_95P                          │              91.2 │              84.7 │   -7.20 │    -6.57
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationMSec_95PWhereIsBackground         │              40.2 │              36.4 │   -9.62 │    -3.87
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationMSec_95PWhereIsGen0               │              45.3 │              41.6 │   -8.23 │    -3.73
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationMSec_Mean                         │              36.9 │              32.9 │   -10.8 │    -3.98
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationMSec_MeanWhereIsEphemeral         │              37.3 │              33.2 │   -11.2 │    -4.17
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationSeconds_Sum                       │              19.5 │              17.4 │   -10.8 │    -2.10
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationSeconds_SumWhereIsBackground      │              9.45 │              8.48 │   -10.3 │   -0.973
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationSeconds_SumWhereIsGen1            │              5.49 │              4.99 │   -8.99 │   -0.493
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationSeconds_SumWhereIsNonBackground   │              10.0 │              8.89 │   -11.3 │    -1.13
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PctTimeInGC_WhereIsNonBackground               │              31.7 │              29.4 │   -7.14 │    -2.26
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PctTimePausedInGC                              │              33.0 │              30.7 │   -6.96 │    -2.30
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  speed                                          │              53.7 │              50.5 │   -5.84 │    -3.13


Stale (Same, or percent difference within 5% margin)

                                          Metric │      Base (run 0) │       New (run 0) │  % Diff │ Abs Diff
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  CountIsBlockingGen2                            │                 0 │                 0 │       0 │        0
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  CountIsGen0                                    │               189 │               189 │       0 │        0
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  CountIsGen1                                    │                81 │                81 │       0 │        0
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  CountUsesLOHCompaction                         │ <not implemented> │ <not implemented> │         │
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  FirstToLastGCSeconds                           │              31.6 │              30.2 │   -4.46 │    -1.41
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  HeapCount                                      │                 6 │                 6 │       0 │        0
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  HeapSizeAfterMB_Max                            │              4419 │              4498 │    1.79 │     79.3
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  HeapSizeAfterMB_Mean                           │              4070 │              4060 │  -0.233 │    -9.47
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  HeapSizeBeforeMB_Max                           │              5673 │              5749 │    1.33 │     75.6
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  HeapSizeBeforeMB_Mean                          │              5282 │              5277 │ -0.0881 │    -4.65
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationMSec_95PWhereIsBlockingGen2       │       <no values> │       <no values> │         │
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationMSec_95PWhereIsGen1               │               100 │              95.9 │   -4.54 │    -4.56
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationSeconds_SumWhereIsBlockingGen2    │                 0 │                 0 │       0 │        0
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PauseDurationSeconds_SumWhereUsesLOHCompaction │ <not implemented> │ <not implemented> │         │
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PctIsEphemeral                                 │              95.4 │              95.7 │   0.355 │    0.338
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PctReductionInHeapSize_Mean                    │              22.8 │              22.9 │   0.515 │    0.117
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PctUsesCompaction                              │              93.6 │              94.0 │   0.355 │    0.332
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PromotedMB_MeanWhereIsBlockingGen2             │       <no values> │       <no values> │         │
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PromotedMB_MeanWhereIsGen0                     │              51.7 │              51.8 │   0.114 │   0.0588
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  PromotedMB_MeanWhereIsGen1                     │               229 │               228 │  -0.420 │   -0.963
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  TotalAllocatedMB                               │          3.23e+05 │          3.22e+05 │  -0.217 │     -701
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  TotalLOHAllocatedMB                            │              8.01 │              8.01 │       0 │        0
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  TotalNonGCSeconds                              │              22.2 │              21.9 │   -1.51 │   -0.335
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  TotalNumberGCs                                 │               283 │               282 │  -0.353 │       -1
  ───────────────────────────────────────────────┼───────────────────┼───────────────────┼─────────┼─────────
  TotalSecondsTaken                              │              32.7 │              31.2 │   -4.58 │    -1.49
```

If we take a look at these numbers, we can see that the performance was very
similar between these two _GCPerfSim_ executables.

In this case, assuming the other tests yielded similar results, we would be
ready to submit our PR to merge these new changes to _GCPerfSim_.

Depending on your use case, you can also do comparisons with different Core Root's
and different configurations as well.
