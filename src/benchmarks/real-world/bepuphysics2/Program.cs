using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DemoBenchmarks;
using System.Collections.Immutable;

public class BepuPhysics2Benchmarks
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher
            .FromAssembly(typeof(BepuPhysics2Benchmarks).Assembly)
            .Run(args, RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(BepuPhysics2Benchmarks).Assembly.Location), "BenchmarkDotNet.Artifacts")),
                    mandatoryCategories: ImmutableHashSet.Create(Categories.BepuPhysics)))
            .ToExitCode();
}
