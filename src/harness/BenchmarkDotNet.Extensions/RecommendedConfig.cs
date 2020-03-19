using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Extensions
{
    public static class RecommendedConfig
    {
        public static IConfig Create(
            DirectoryInfo artifactsPath,
            ImmutableHashSet<string> mandatoryCategories,
            int? partitionCount = null,
            int? partitionIndex = null,
            Job job = null)
        {
            if (job is null)
            {
                job = Job.Default
                    .WithWarmupCount(1) // 1 warmup is enough for our purpose
                    .WithIterationTime(TimeInterval.FromMilliseconds(250)) // the default is 0.5s per iteration, which is slighlty too much for us
                    .WithMinIterationCount(15)
                    .WithMaxIterationCount(20) // we don't want to run more that 20 iterations
                    .DontEnforcePowerPlan(); // make sure BDN does not try to enforce High Performance power plan on Windows

                // See https://github.com/dotnet/roslyn/issues/42393
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    job = job.With(new Argument[] { new MsBuildArgument("/p:DebugType=portable") });
                }
            }

            return DefaultConfig.Instance
                .With(job.AsDefault()) // tell BDN that this are our default settings
                .WithArtifactsPath(artifactsPath.FullName)
                .With(MemoryDiagnoser.Default) // MemoryDiagnoser is enabled by default
                .With(new OperatingSystemFilter())
                .With(new PartitionFilter(partitionCount, partitionIndex))
                .With(JsonExporter.Full) // make sure we export to Json
                .With(new PerfLabExporter())
                .With(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .With(TooManyTestCasesValidator.FailOnError)
                .With(new UniqueArgumentsValidator()) // don't allow for duplicated arguments #404
                .With(new MandatoryCategoryValidator(mandatoryCategories))
                .With(SummaryStyle.Default.WithMaxParameterColumnWidth(36)); // the default is 20 and trims too aggressively some benchmark results
        }
    }
}
