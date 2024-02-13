(This file is generated by `py . lint`)

The following list does not include aggregate metrics.

For any single-heap-metric, you can add an underscore `_` followed by an aggregate name
to get a single-gc-metric, for each aggregate name in ('Mean', 'Max', 'Min', 'Sum', '95P', 'Stdev').
For example, the single-gc-metric `Gen0SizeAfterMB_Mean` is the mean of the single-heap-metric
`Gen0SizeAfterMB` taken from each heap within that GC.

You can similarly get a run metric by aggregating GC metrics, even if that was already an aggregate,
as in `Gen0SizeAfterMB_Mean_Mean`, which is the mean of the single-heap-metric `Gen0SizeAfterMB`,
averaged over each heap within a GC, averaged over all GCs.

Also, while taking an aggregate, you can add `Where` followed by the name of a boolean metric.
For example, the run-metric `PauseDurationMSec_MeanWhereIsBlockingGen2` is the mean of
the single-gc-metric `PauseDurationMSec`,
considering only the GCs where another single-gc-metric `WhereIsBlockingGen2` returns true.

You can also aggregate a boolean metric by prefixing it with `Pct`.
For example, `PctIsBlockingGen2` is a run-metric aggregating the single-gc-metric
`IsBlockingGen2`, giving the percentage of GCs where that returned true.
A boolean aggregate can have a `Where` part too, as in `PctIsNonConcurrentWhereIsGen2`,
which tells you what percentage of Gen2 gcs where blocking.


# single-heap-metrics

no bool metrics

## float metrics

AllGensBudgetMB
AllGensFragmentationMB
AllGensFreeListSpaceAfterMB
AllGensFreeListSpaceBeforeMB
AllGensFreeObjSpaceAfterMB
AllGensFreeObjSpaceBeforeMB
AllGensInMB
AllGensObjSizeAfterMB
AllGensObjSpaceBeforeMB
AllGensOutMB
AllGensSizeAfterMB
AllGensSizeBeforeMB
BudgetMB
FragmentationMB
FreeListAllocated
FreeListConsumed
FreeListRejected
FreeListSpaceAfterMB
FreeListSpaceBeforeMB
FreeObjSpaceAfterMB
FreeObjSpaceBeforeMB
Gen0BudgetMB
Gen0FragmentationMB
Gen0FreeListSpaceAfterMB
Gen0FreeListSpaceBeforeMB
Gen0FreeObjSpaceAfterMB
Gen0FreeObjSpaceBeforeMB
Gen0FullnessPercent
Gen0InMB
Gen0NonePinnedSurv
Gen0ObjSizeAfterMB
Gen0ObjSpaceBeforeMB
Gen0OutMB
Gen0PinnedSurv
Gen0SizeAfterMB
Gen0SizeBeforeMB
Gen0SurvRate
Gen1BudgetMB
Gen1FragmentationMB
Gen1FreeListSpaceAfterMB
Gen1FreeListSpaceBeforeMB
Gen1FreeObjSpaceAfterMB
Gen1FreeObjSpaceBeforeMB
Gen1InMB
Gen1NonePinnedSurv
Gen1ObjSizeAfterMB
Gen1ObjSpaceBeforeMB
Gen1OutMB
Gen1PinnedSurv
Gen1SizeAfterMB
Gen1SizeBeforeMB
Gen1SurvRate
Gen2BudgetMB
Gen2FragmentationMB
Gen2FreeListSpaceAfterMB
Gen2FreeListSpaceBeforeMB
Gen2FreeObjSpaceAfterMB
Gen2FreeObjSpaceBeforeMB
Gen2InMB
Gen2NonePinnedSurv
Gen2ObjSizeAfterMB
Gen2ObjSpaceBeforeMB
Gen2OutMB
Gen2PinnedSurv
Gen2SizeAfterMB
Gen2SizeBeforeMB
Gen2SurvRate
InMB
LohBudgetMB
LohFragmentationMB
LohFreeListSpaceAfterMB
LohFreeListSpaceBeforeMB
LohFreeObjSpaceAfterMB
LohFreeObjSpaceBeforeMB
LohInMB
LohNonePinnedSurv
LohObjSizeAfterMB
LohObjSpaceBeforeMB
LohOutMB
LohPinnedSurv
LohSizeAfterMB
LohSizeBeforeMB
LohSurvRate
MarkFQMSec
MarkFQPromotedMB
MarkHandlesMSec
MarkHandlesPromotedMB
MarkOlderMSec
MarkOlderPromotedMB
MarkOverflowMSec
MarkOverflowPromotedMB
MarkSizedRefMSec
MarkSizedRefPromotedMB
MarkStackMSec
MarkStackPromotedMB
MaxIndividualJoinMSec
MedianIndividualJoinMSec
ObjSizeAfterMB
ObjSpaceBeforeMB
OutMB
SizeAfterMB
SizeBeforeMB
TotalJoinMSec
TotalMarkMSec
TotalMarkPromotedMB
TotalStolenMSec
	Sum of each time the processor was stolen for this heap's thread.
adjust_handle_age_compact
adjust_handle_age_sweep
after_absorb
after_commit_soh_no_gc
after_ephemeral_sweep
after_profiler_heap_walk
after_reset
allow_fgc
begin_mark_phase
begin_relocate_phase
bgc_after_ephemeral
bgc_sweep
compact
concurrent_overflow
decide_on_compaction
disable_software_write_watch
done
expand_loh_no_gc
final_no_gc
generation_determined
heap_verify
idle_for_no_good_reason
init
init_cpu_mapping
mark
minimal_gc
null_dead_long_weak
null_dead_short_weak
null_dead_syncblk
plan
post_gc
rearrange_segs_compaction
relocate
relocate_phase_done
rescan_dependent_handles
restart_ee
restart_ee_verify
restarting
scan_dependent_handles
scan_finalization
scan_sizedref_done
set_state_free
single_threaded
start_bgc
stolen
suspend_ee
suspend_ee_verify
sweep
update_card_bundles
verify_copy_table
verify_objects_done
waiting_in_join
working


# single-gc-metrics

## bool metrics

CompactsBecause_fragmented_gen0
CompactsBecause_high_frag
CompactsBecause_high_mem_frag
CompactsBecause_high_mem_load
CompactsBecause_induced_compacting
CompactsBecause_last_gc
CompactsBecause_loh_forced
CompactsBecause_low_ephemeral
CompactsBecause_no_gaps
CompactsBecause_no_gc_mode
CompactsBecause_vhigh_mem_frag
ExpandsBecause_expand_new_seg
ExpandsBecause_expand_new_set_ep
ExpandsBecause_expand_next_full_gc
ExpandsBecause_expand_no_memory
ExpandsBecause_expand_reuse_bestfit
ExpandsBecause_expand_reuse_normal
IsBackground
IsBlockingGen2
IsConcurrent
IsEphemeral
IsForeground
IsGen0
IsGen1
IsGen2
IsNonBackground
IsNonConcurrent
Reason_Is_alloc_loh
Reason_Is_alloc_soh
Reason_Is_bgc_stepping
Reason_Is_bgc_tuning_loh
Reason_Is_bgc_tuning_soh
Reason_Is_empty
Reason_Is_gcstress
Reason_Is_induced
Reason_Is_induced_compacting
Reason_Is_induced_noforce
Reason_Is_lowmemory
Reason_Is_lowmemory_blocking
Reason_Is_lowmemory_host
Reason_Is_lowmemory_host_blocking
Reason_Is_oos_loh
Reason_Is_oos_soh
Reason_Is_pm_full_gc
UsesCardBundles
UsesCompaction
UsesDemotion
UsesElevation
UsesLOHCompaction
UsesPromotion

## float metrics

AllocRateMBSec
AllocedMBAccumulated
AllocedSinceLastGCMB
BGCFinalPauseMSec
BGCLohConcurrentRevisitedPages
BGCSohConcurrentRevisitedPages
DurationMSec
DurationSeconds
DurationSinceLastRestartMSec
EndMSec
GCCpuMSec
Gen0BudgetMB
Gen0FragmentationMB
Gen0FragmentationPercent
Gen0InMB
Gen0ObjSizeAfterMB
Gen0PromotedMB
Gen0SizeAfterMB
Gen0SizeBeforeMB
Gen0SurvivalPercent
Gen1BudgetMB
Gen1FragmentationMB
Gen1FragmentationPercent
Gen1InMB
Gen1ObjSizeAfterMB
Gen1PromotedMB
Gen1SizeAfterMB
Gen1SizeBeforeMB
Gen1SurvivalPercent
Gen2BudgetMB
Gen2FragmentationMB
Gen2FragmentationPercent
Gen2InMB
Gen2ObjSizeAfterMB
Gen2PromotedMB
Gen2SizeAfterMB
Gen2SizeBeforeMB
Gen2SurvivalPercent
GenLargeObjBudgetMB
GenLargeObjFragmentationMB
GenLargeObjFragmentationPercent
GenLargeObjInMB
GenLargeObjObjSizeAfterMB
GenLargeObjPromotedMB
GenLargeObjSizeAfterMB
GenLargeObjSizeBeforeMB
GenLargeObjSurvivalPercent
Generation
HeapCount
HeapSizeAfterMB
HeapSizeBeforeMB
HeapSizePeakMB
Index
LastPerHeapHistToEndMSec
MaxBGCWaitMSec
MbAllocatedOnLOHSinceLastGen2Gc
MbAllocatedOnSOHSinceLastSameGenGc
MemoryPressure
Number
PauseDurationMSec
PauseDurationSeconds
PauseEndMSec
PauseStartMSec
PauseTimePercentageSinceLastGC
PctReductionInHeapSize
PctTimeInThisGcSinceLastGc
PinnedObjectPercentage
PinnedObjectSizes
ProcessCpuMSec
PromotedGBPerSec
PromotedMB
PromotedMBPerSec
RatioPeakAfter
SecondsPerPromotedGB
StartMSec
SuspendDurationMSec
SuspendToGCStartMSec
TotalGCTime
	WARN: Only works in increments of 1MS, may error on smaller GCs
Type
	Value of GCType enum


# run-metrics

no bool metrics

## float metrics

### float metrics that only require test status

FinalFragmentationGB
FinalHeapSizeGB
FinalTotalMemoryGB
Gen0CollectionCount
Gen0Size
Gen1CollectionCount
Gen2CollectionCount
InternalSecondsTaken
NumCreatedWithFinalizers
NumFinalized
ThreadCount
TotalSecondsTaken

### float metrics that require a trace file

FinalYoungestDesiredMB
FirstEventToFirstGCSeconds
FirstToLastGCSeconds
HeapCount
PctTimePausedInGC
TotalAllocatedMB
TotalLOHAllocatedMB
TotalNonGCSeconds
ready_stolen_cpu_fraction_max
ready_stolen_cpu_fraction_mean
single_threaded_stolen_cpu_fraction_max
single_threaded_stolen_cpu_fraction_mean
unknown_stolen_cpu_fraction_max
unknown_stolen_cpu_fraction_mean
waiting_in_join_stolen_cpu_fraction_max
waiting_in_join_stolen_cpu_fraction_mean
waiting_in_restart_stolen_cpu_fraction_max
waiting_in_restart_stolen_cpu_fraction_mean