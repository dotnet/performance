using DataTransferContracts;
using MarkdownLog;
using Perfolizer.Mathematics.SignificanceTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResultsComparer
{
    internal static class MultipleInputsComparer
    {
        internal static void Compare(MultipleInputsOptions args)
        {
            Console.WriteLine("# Legend");
            Console.WriteLine();
            Console.WriteLine($"* Statistical Test threshold: {args.StatisticalTestThreshold}, the noise filter: {args.NoiseThreshold}");
            Console.WriteLine("* Result is conslusion: Slower|Faster|Same");
            Console.WriteLine("* Base is median base execution time in nanoseconds");
            Console.WriteLine("* Diff is median diff execution time in nanoseconds");
            Console.WriteLine("* Ratio = Base/Diff (the higher the better)");
            Console.WriteLine("* Alloc Delta = Allocated bytes diff - Allocated bytes base (the lower the better)");
            Console.WriteLine("* Base V = Base Runtime Version");
            Console.WriteLine("* Diff V = Diff Runtime Version");
            Console.WriteLine();

            Stats stats = new Stats();

            foreach (var benchmarkResults in args.BasePaths
                .SelectMany((basePath, index) => GetResults(basePath, args.DiffPaths.ElementAt(index), args, stats))
                .GroupBy(result => result.id, StringComparer.InvariantCulture)
                //.Where(group => group.Any(result => result.conclusion == EquivalenceTestConclusion.Slower))
                //.Where(group => !group.All(result => result.conclusion == EquivalenceTestConclusion.Same || result.conclusion == EquivalenceTestConclusion.Base)) // we are not interested in things that did not change
                .Take(args.TopCount ?? int.MaxValue)
                .OrderBy(group => group.Sum(result => Score(result.conclusion, result.baseEnv, result.baseResult, result.diffResult))))
            {
                if (args.PrintStats)
                {
                    stats.Print();
                }

                Console.WriteLine($"## {benchmarkResults.Key}");
                Console.WriteLine();

                var data = benchmarkResults
                    .OrderBy(result => Importance(result.baseEnv))
                    .Select(result => new
                    {
                        Conclusion = result.conclusion,
                        BaseMedian = result.baseResult.Statistics.Median,
                        DiffMedian = result.diffResult.Statistics.Median,
                        Ratio = result.baseResult.Statistics.Median / result.diffResult.Statistics.Median,
                        AllocatedDiff = GetAllocatedDiff(result.diffResult, result.baseResult),
                        Modality = Helper.GetModalInfo(result.baseResult) ?? Helper.GetModalInfo(result.diffResult),
                        OperatingSystem = Stats.GetSimplifiedOSName(result.baseEnv.OsVersion),
                        Architecture = result.baseEnv.Architecture,
                        ProcessorName = result.baseEnv.ProcessorName,
                        BaseRuntimeVersion = GetSimplifiedRuntimeVersion(result.baseEnv.RuntimeVersion),
                        DiffRuntimeVersion = GetSimplifiedRuntimeVersion(result.diffEnv.RuntimeVersion),
                    })
                    .ToArray();

                var table = data.ToMarkdownTable().WithHeaders("Result", "Base", "Diff", "Ratio", "Alloc Delta", "Modality", "Operating System", "Bit", "Processor Name", "Base V", "Diff V");

                foreach (var line in table.ToMarkdown().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                    Console.WriteLine($"| {line.TrimStart()}|"); // the table starts with \t and does not end with '|' and it looks bad so we fix it

                Console.WriteLine();
            }
        }

        private static string GetAllocatedDiff(Benchmark diffResult, Benchmark baseResult)
        {
            long baseline = baseResult.Memory.BytesAllocatedPerOperation;
            if (baseline == 0)
                baseline = GetMetricValue(baseResult);
            long diff = diffResult.Memory.BytesAllocatedPerOperation;
            if (diff == 0)
                diff = GetMetricValue(diffResult);

            return (diff - baseline).ToString("+0;-#");

            static long GetMetricValue(Benchmark result)
            {
                if (result.Metrics == null)
                    return 0;

                double value = result.Metrics.Single(metric => metric.Descriptor.Id == "Allocated Memory").Value;
                if (value < 1.0)
                    return 0;

                return (long)value;
            }
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion, HostEnvironmentInfo baseEnv, HostEnvironmentInfo diffEnv)> GetResults(
            string basePath, string diffPath, MultipleInputsOptions args, Stats stats)
        {
            foreach (var info in ReadResults(basePath, diffPath, args.Filters)
                .Where(result => result.baseResult.Statistics != null && result.diffResult.Statistics != null)) // failures
            {
                if (info.baseEnv.Architecture != info.diffEnv.Architecture)
                    throw new InvalidOperationException("Use ResultsComparer to compare different Architectures");
                //if (info.baseEnv.OsVersion != info.diffEnv.OsVersion)
                //    throw new InvalidOperationException("Use ResultsComparer to compare different OS Versions");
                //if (info.baseEnv.ProcessorName != info.diffEnv.ProcessorName)
                //    throw new InvalidOperationException("Use ResultsComparer to compare different Processors");

                var baseValues = info.baseResult.Statistics.OriginalValues;
                var diffValues = info.diffResult.Statistics.OriginalValues;

                var userTresholdResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, args.StatisticalTestThreshold);
                var noiseResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, args.NoiseThreshold);

                var conclusion = noiseResult.Conclusion == EquivalenceTestConclusion.Same // filter noise (0.20 ns vs 0.25ns etc)
                    ? noiseResult.Conclusion
                    : userTresholdResult.Conclusion;

                stats.Record(conclusion, info.baseEnv, info.baseResult);

                yield return (info.id, info.baseResult, info.diffResult, conclusion, info.baseEnv, info.diffEnv);
            }
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult, HostEnvironmentInfo baseEnv, HostEnvironmentInfo diffEnv)>
            ReadResults(string basePath, string diffPath, IEnumerable<Regex> filters)
        {
            var baseFiles = Helper.GetFilesToParse(basePath);
            var diffFiles = Helper.GetFilesToParse(diffPath);

            if (!baseFiles.Any() || !diffFiles.Any())
                throw new ArgumentException($"Provided paths contained no {Helper.FullBdnJsonFileExtension} files.");

            var baseResults = baseFiles.Select(Helper.ReadFromFile);
            var diffResults = diffFiles.Select(Helper.ReadFromFile);

            var benchmarkIdToDiffResults = new Dictionary<string, (Benchmark result, HostEnvironmentInfo env)>(StringComparer.InvariantCulture);

            foreach (var diffResult in diffResults)
            {
                foreach (var diffBenchmark in diffResult.Benchmarks.Where(benchmarkResult => !filters.Any() || filters.Any(filter => filter.IsMatch(benchmarkResult.FullName))))
                {
                    benchmarkIdToDiffResults.Add(diffBenchmark.FullName, (diffBenchmark, diffResult.HostEnvironmentInfo));
                }
            }

            foreach (var baseResult in baseResults)
            {
                foreach (var baseBenchmark in baseResult.Benchmarks.Where(result => benchmarkIdToDiffResults.ContainsKey(result.FullName)))
                {
                    (Benchmark diffBenchmark, HostEnvironmentInfo diffEnv) = benchmarkIdToDiffResults[baseBenchmark.FullName];

                    yield return (baseBenchmark.FullName, baseBenchmark, diffBenchmark, baseResult.HostEnvironmentInfo, diffEnv);
                }
            }
        }

        private static double Score(EquivalenceTestConclusion conclusion, HostEnvironmentInfo env, Benchmark baseResult, Benchmark diffResult)
        {
            switch (conclusion)
            {
                case EquivalenceTestConclusion.Base:
                case EquivalenceTestConclusion.Same:
                case EquivalenceTestConclusion.Unknown:
                    return 0;
                case EquivalenceTestConclusion.Faster:
                    double improvementXtimes = baseResult.Statistics.Median / diffResult.Statistics.Median;
                    return (double.IsNaN(improvementXtimes) || double.IsInfinity(improvementXtimes))
                        ? Importance(env) * 10.0
                        : Importance(env) * Math.Min(improvementXtimes, 10.0);
                case EquivalenceTestConclusion.Slower:
                    double regressionXtimes = diffResult.Statistics.Median / baseResult.Statistics.Median;
                    return (double.IsNaN(regressionXtimes) || double.IsInfinity(regressionXtimes))
                        ? Importance(env) * -10.0
                        : Importance(env) * Math.Min(regressionXtimes, 10.0) * -1.0;
                default:
                    throw new NotSupportedException($"{conclusion} is not supported");
            }
        }

        private static int Importance(HostEnvironmentInfo env)
        {
            // it's not any kind of official Microsoft priority, just the way I see them:
            // 1. x64 Windows
            // 2. x64 Linux
            // 3. arm64 Linux
            // 4. arm64 Windows
            // 5. x86 Windows
            // 6. arm Windows
            // 7. x64 macOS

            if (env.Architecture == "X64" && env.OsVersion.StartsWith("Windows", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            else if (env.Architecture == "X64" && !env.OsVersion.StartsWith("macOS", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
            else if (env.Architecture == "Arm64" && !env.OsVersion.StartsWith("Windows", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }
            else if (env.Architecture == "Arm64")
            {
                return 4;
            }
            else if (env.Architecture == "X86")
            {
                return 5;
            }
            else if (env.Architecture == "Arm")
            {
                return 6;
            }
            else
            {
                return 7;
            }
        }

        private static string GetSimplifiedRuntimeVersion(string text)
        {
            if (text.StartsWith(".NET Core 3", StringComparison.OrdinalIgnoreCase))
            {
                // it's something like ".NET Core 3.1.6 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.31603)"
                // and what we care about is "3.1.6"
                return text.Substring(".NET Core ".Length, "3.1.X".Length);
            }
            else
            {
                // it's something like ".NET 6.0.0 (6.0.21.35216)"
                // and what we care about is "6.0.21.35216"
                int index = text.IndexOf('(');
                return text.Substring(index + 1, text.Length - index - 2);
            }
        }
    }
}
