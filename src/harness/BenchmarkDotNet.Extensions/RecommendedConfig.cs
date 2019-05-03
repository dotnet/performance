using System.Collections.Immutable;
using System.IO;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Extensions
{
    public static class RecommendedConfig
    {
        public static IConfig Create(DirectoryInfo artifactsPath, ImmutableHashSet<string> mandatoryCategories, int? partitionCount = null, int? partitionIndex = null)
            => DefaultConfig.Instance
                .With(Job.Default
                    .WithWarmupCount(1) // 1 warmup is enough for our purpose
                    .WithIterationTime(TimeInterval.FromMilliseconds(250)) // the default is 0.5s per iteration, which is slighlty too much for us
                    .WithMinIterationCount(15)
                    .WithMaxIterationCount(20) // we don't want to run more that 20 iterations
                    .AsDefault()) // tell BDN that this are our default settings
                .WithArtifactsPath(artifactsPath.FullName)
                .With(MemoryDiagnoser.Default) // MemoryDiagnoser is enabled by default
                .With(new OperatingSystemFilter())
                .With(new PartitionFilter(partitionCount, partitionIndex))
                .With(JsonExporter.Full) // make sure we export to Json (for BenchView integration purpose)
                .With(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .With(TooManyTestCasesValidator.FailOnError)
                .With(new UniqueArgumentsValidator()) // don't allow for duplicated arguments #404
                .With(new MandatoryCategoryValidator(mandatoryCategories));
    }
}