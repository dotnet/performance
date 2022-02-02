using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace BenchmarkDotNet.Extensions.Tests
{
    public class PartitionFilterTests
    {
        [Fact]
        public void NoBenchmarksAreOmitted()
        {
            const int PartitionCount = 5; // same as in eng/performance/helix.proj

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
                .Single(method => method.Name == nameof(NoBenchmarksAreOmitted));

            Descriptor target = new (typeof(PartitionFilterTests), publicMethod);
            ParameterInstances parameterInstances = new (Array.Empty<ParameterInstance>());
            ImmutableConfig config = ManualConfig.CreateEmpty().CreateImmutableConfig();

            for (int i = 0; i < count; i++)
            {
                yield return BenchmarkCase.Create(target, Job.Default, parameterInstances, config);
            }
        }
    }
}
