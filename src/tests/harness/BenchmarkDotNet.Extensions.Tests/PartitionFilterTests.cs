using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using MicroBenchmarks;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace BenchmarkDotNet.Extensions.Tests
{
    public class PartitionFilterTests
    {
        private const int PartitionCount = 30; // same as used by the runtime repo

        [Fact]
        public void NoBenchmarksAreOmitted_MockData()
        {
            Dictionary<BenchmarkCase, int> hits = GenerateBenchmarkCases(4_000).ToDictionary(benchmark => benchmark, _ => 0);

            for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
            {
                PartitionFilter filter = new (PartitionCount, partitionIndex);

                foreach (BenchmarkCase benchmark in hits.Keys)
                {
                    if (filter.Predicate(benchmark))
                    {
                        hits[benchmark] += 1;
                    }
                }
            }

            Assert.All(hits.Values, hitCount => Assert.Equal(1, hitCount));
        }

        private static IEnumerable<BenchmarkCase> GenerateBenchmarkCases(int count)
        {
            MethodInfo publicMethod = typeof(PartitionFilterTests)
                .GetMethods()
                .Single(method => method.Name == nameof(NoBenchmarksAreOmitted_MockData));

            Descriptor target = new (typeof(PartitionFilterTests), publicMethod);
            ParameterInstances parameterInstances = new (Array.Empty<ParameterInstance>());
            ImmutableConfig config = ManualConfig.CreateEmpty().CreateImmutableConfig();

            for (int i = 0; i < count; i++)
            {
                yield return BenchmarkCase.Create(target, Job.Default, parameterInstances, config);
            }
        }

        [Fact]
        public void NoBenchmarksAreOmitted_RealData()
        {
            ILogger nullLogger = new NullLogger();
            IConfig recommendedConfig = RecommendedConfig.Create(
                artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(PartitionFilterTests).Assembly.Location)!, "BenchmarkDotNet.Artifacts")),
                mandatoryCategories: ImmutableHashSet.Create(Categories.Libraries, Categories.Runtime, Categories.ThirdParty));
            (bool isSuccess, IConfig parsedConfig, var _) = ConfigParser.Parse(new string[] { "--filter", "*" }, nullLogger, recommendedConfig);
            Assert.True(isSuccess);

            Assembly microbenchmarksAssembly = typeof(Categories).Assembly;
            (bool allTypesValid, IReadOnlyList<Type> runnable) = Running.TypeFilter.GetTypesWithRunnableBenchmarks(
                Array.Empty<Type>(),
                new Assembly[1] { microbenchmarksAssembly },
                nullLogger);
            Assert.True(allTypesValid);

            BenchmarkRunInfo[] allBenchmarks = GetAllBenchmarks(parsedConfig, runnable);
            Dictionary<string, int> idToPartitionIndex = new ();

            for (int i = 0; i < 10; i++)
            {
                Dictionary<string, int> hits = allBenchmarks
                    .SelectMany(benchmark => benchmark.BenchmarksCases)
                    .ToDictionary(benchmarkCase => GetId(benchmarkCase), _ => 0);

                for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
                {
                    PartitionFilter filter = new(PartitionCount, partitionIndex);

                    foreach (BenchmarkCase benchmark in GetAllBenchmarks(parsedConfig, runnable).SelectMany(benchmark => benchmark.BenchmarksCases))
                    {
                        if (filter.Predicate(benchmark))
                        {
                            string id = GetId(benchmark);

                            hits[id] += 1;

                            if (idToPartitionIndex.ContainsKey(id))
                            {
                                Assert.Equal(partitionIndex, idToPartitionIndex[id]);
                            }
                            else
                            {
                                idToPartitionIndex.Add(id, partitionIndex);
                            }
                        }
                    }
                }

                Assert.All(hits.Values, hitCount => Assert.Equal(1, hitCount));
            }

            static BenchmarkRunInfo[] GetAllBenchmarks(IConfig parsedConfig, IReadOnlyList<Type> runnable)
            {
                return runnable
                    .Select(type => BenchmarkConverter.TypeToBenchmarks(type, parsedConfig))
                    .Where(benchmark => benchmark.BenchmarksCases.Any())
                    .ToArray();
            }

            static string GetId(BenchmarkCase benchmark) => $"{benchmark.Descriptor.Type.Namespace}{benchmark.DisplayInfo}";
        }

        private class NullLogger : ILogger
        {
            public string Id => string.Empty;
            public int Priority => default;
            public void Flush() { }
            public void Write(LogKind logKind, string text) { }
            public void WriteLine() { }
            public void WriteLine(LogKind logKind, string text) { }
        }
    }
}
