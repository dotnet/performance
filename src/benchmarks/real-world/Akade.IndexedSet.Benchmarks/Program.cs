using Akade.IndexedSet.Benchmarks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using System.Collections.Immutable;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "BenchmarkDotNet.Artifacts")),
                    mandatoryCategories: ImmutableHashSet.Create(Categories.AkadeIndexedSet)));