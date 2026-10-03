using Akade.IndexedSet.Benchmarks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using System.Collections.Immutable;

// Use RunAsync (not Run) so BDN does not install its single-threaded
// BenchmarkDotNetSynchronizationContext on the entrypoint thread.
var summaries = await BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAsync(args, RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "BenchmarkDotNet.Artifacts")),
                    mandatoryCategories: ImmutableHashSet.Create(Categories.AkadeIndexedSet)))
    .ConfigureAwait(false);
return summaries.ToExitCode();