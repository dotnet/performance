using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using System.Linq;

[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 10)]
[EtwProfiler]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CustomEventsBenchmarks
{
    private static readonly string loh = "-tc 28 -tagb 540 -tlgb 2 -lohar 100 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 50 -lohsi 50 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -ramb 20 -rlmb 2 -allocType reference -testKind time -rlohsi 50";
    private static readonly string normal = "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 50 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -ramb 20 -rlmb 2 -allocType reference -testKind time";
    private static readonly string soh_pinning = "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 50 -lohsi 0 -pohsi 0 -sohpi 100 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -ramb 20 -rlmb 2 -allocType reference -testKind time";
    private static readonly string poh = "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 100 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 50 -lohsi 0 -pohsi 100 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -ramb 20 -rlmb 2 -allocType reference -testKind time -rpohsi 50";


    [BenchmarkCategory("loh"), Benchmark(Baseline = true)]
    public void RunLOH() => MemoryAlloc.Test(loh.Split(' ').ToArray());

    [BenchmarkCategory("normal"), Benchmark(Baseline = true)]
    public void RunNormal() => MemoryAlloc.Test(normal.Split(' ').ToArray());

    [BenchmarkCategory("soh_pinning"), Benchmark(Baseline = true)]
    public void RunSOH_Pinning() => MemoryAlloc.Test(soh_pinning.Split(' ').ToArray());

    [BenchmarkCategory("poh"), Benchmark(Baseline = true)]
    public void RunPOH() => MemoryAlloc.Test(poh.Split(' ').ToArray());

    [BenchmarkCategory("loh"), Benchmark]
    public void RunLOHWithCustomEvents()
    {
        using var listener = new GCEventListener();
        MemoryAlloc.Test(loh.Split(' ').ToArray());
    }

    [BenchmarkCategory("normal"), Benchmark]
    public void RunNormalWithCustomEvents()
    {
        using var listener = new GCEventListener();
        MemoryAlloc.Test(normal.Split(' ').ToArray());
    }

    [BenchmarkCategory("soh_pinning"), Benchmark]
    public void RunSOH_PinningWithCustomEvents()
    {
        using var listener = new GCEventListener();
        MemoryAlloc.Test(soh_pinning.Split(' ').ToArray());
    }

    [BenchmarkCategory("poh"), Benchmark]
    public void RunPOHWithCustomEvents()
    {
        using var listener = new GCEventListener();
        MemoryAlloc.Test(poh.Split(' ').ToArray());
    }
}

public static class Harness
{
    public static void Main(string[] args)
    {
        //MemoryAlloc.Test(args);
        var summary = BenchmarkRunner.Run<CustomEventsBenchmarks>();
    }
}