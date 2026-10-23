using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DemoBenchmarks;
using System.Collections.Immutable;
using System.Threading.Tasks;

public class BepuPhysics2Benchmarks
{
    // Use RunAsync (not Run) so BDN does not install its single-threaded
    // BenchmarkDotNetSynchronizationContext on the entrypoint thread.
    public static async Task<int> Main(string[] args)
    {
        var summaries = await BenchmarkSwitcher
            .FromAssembly(typeof(BepuPhysics2Benchmarks).Assembly)
            .RunAsync(args, RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(BepuPhysics2Benchmarks).Assembly.Location), "BenchmarkDotNet.Artifacts")),
                    mandatoryCategories: ImmutableHashSet.Create(Categories.BepuPhysics)))
            .ConfigureAwait(false);
        return summaries.ToExitCode();
    }
}
