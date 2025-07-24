using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using System.Linq;

public static class ConsoleArgs
{
    public static string loh = "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 50 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -ramb 20 -rlmb 2 -allocType reference -testKind time";
}

[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 3)]
[EtwProfiler]
[MemoryDiagnoser]
public class CustomEventsBenchmarks
{
    private readonly string[] args = ConsoleArgs.loh.Split(' ')
        .ToArray(); 

    [Benchmark(Baseline = true)]
    public void RunTest()
    {
        MemoryAlloc.Test(args);
    }

    [Benchmark]
    public void RunTestWithCustomEvents()
    {
        using var listener = new GCEventListener();
        MemoryAlloc.Test(args);
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