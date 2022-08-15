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
        private static readonly string[] Headers = new[] { "Result", "Base", "Diff", "Ratio", "Alloc Delta", "Operating System", "Bit", "Processor Name", "Modality" };
        private static readonly string[] HeadersRatioOnly = new[] { "Result", "Ratio", "Alloc Delta", "Operating System", "Bit", "Processor Name", "Modality" };

        internal static void Compare(MultipleInputsOptions args)
        {
            Console.WriteLine("# Legend");
            Console.WriteLine();
            Console.WriteLine($"* Statistical Test threshold: {args.StatisticalTestThreshold}, the noise filter: {args.NoiseThreshold}");
            Console.WriteLine($"* Result is conclusion: Slower|Faster|Same|Noise|Unknown. Noise means that the difference was larger than {args.StatisticalTestThreshold} but not {args.NoiseThreshold}.");
            if (!args.RatioOnly) 
            {
                Console.WriteLine($"* Base is median base execution time in nanoseconds for {args.BasePattern}");
                Console.WriteLine($"* Diff is median diff execution time in nanoseconds for {args.DiffPattern}");
            }
            Console.WriteLine("* Ratio = Base/Diff (the higher the better).");
            Console.WriteLine("* Alloc Delta = Allocated bytes diff - Allocated bytes base (the lower the better)");
            Console.WriteLine();

            Stats stats = new Stats();

            foreach (var benchmarkResults in args.BasePaths
                .SelectMany((basePath, index) => GetResults(basePath, args.DiffPaths.ElementAt(index), args, stats))
                .GroupBy(result => result.id, StringComparer.InvariantCulture)
                .Take(args.TopCount ?? int.MaxValue)
                .OrderBy(group => group.Sum(result => Score(result.conclusion, result.baseEnv, result.baseResult, result.diffResult))))
            {
                if (args.PrintStats)
                {
                    stats.Print();
                }

                Console.WriteLine($"## {benchmarkResults.Key}");
                Console.WriteLine();

                Table table = null;
                if (args.RatioOnly) 
                {
                    var data = benchmarkResults
                        .OrderBy(result => Order(result.baseEnv))
                        .Select(result => new
                        {
                            Conclusion = result.conclusion == Stats.Noise ? "Noise" : result.conclusion.ToString(),
                            Ratio = GetRatio(result),
                            AllocatedDiff = GetAllocatedDiff(result.diffResult, result.baseResult),
                            OperatingSystem = Stats.GetSimplifiedOSName(result.baseEnv.OsVersion),
                            Architecture = result.baseEnv.Architecture,
                            ProcessorName = result.baseEnv.ProcessorName,
                            Modality = Helper.GetModalInfo(result.baseResult) ?? Helper.GetModalInfo(result.diffResult),
                        })
                        .ToArray();
                    table = data.ToMarkdownTable().WithHeaders(HeadersRatioOnly);
                } 
                else 
                {
                    var data = benchmarkResults
                        .OrderBy(result => Order(result.baseEnv))
                        .Select(result => new
                        {
                            Conclusion = result.conclusion == Stats.Noise ? "Noise" : result.conclusion.ToString(),
                            BaseMedian = result.baseResult.Statistics.Median,
                            DiffMedian = result.diffResult.Statistics.Median,
                            Ratio = GetRatio(result),
                            AllocatedDiff = GetAllocatedDiff(result.diffResult, result.baseResult),
                            OperatingSystem = Stats.GetSimplifiedOSName(result.baseEnv.OsVersion),
                            Architecture = result.baseEnv.Architecture,
                            ProcessorName = result.baseEnv.ProcessorName,
                            Modality = Helper.GetModalInfo(result.baseResult) ?? Helper.GetModalInfo(result.diffResult),
                        })
                        .ToArray();
                    table = data.ToMarkdownTable().WithHeaders(Headers);
                }

                foreach (var line in table.ToMarkdown().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                    Console.WriteLine($"| {line.TrimStart()}|"); // the table starts with \t and does not end with '|' and it looks bad so we fix it

                Console.WriteLine();
            }
        }

        private static string GetRatio((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion, HostEnvironmentInfo baseEnv, HostEnvironmentInfo diffEnv) result)
        {
            double ratio = result.baseResult.Statistics.Median / result.diffResult.Statistics.Median;

            if (double.IsNaN(ratio) || result.conclusion == Stats.Noise)
            {
                return "-";
            }

            return ratio.ToString("0.00");
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

                // filter noise (0.20 ns vs 0.25ns is 25% difference)
                var conclusion = userTresholdResult.Conclusion != EquivalenceTestConclusion.Same && noiseResult.Conclusion == EquivalenceTestConclusion.Same
                    ? Stats.Noise
                    : userTresholdResult.Conclusion == EquivalenceTestConclusion.Base ? EquivalenceTestConclusion.Same : userTresholdResult.Conclusion;

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
                case Stats.Noise:
                    return 0;
                case EquivalenceTestConclusion.Faster:
                    double improvementXtimes = baseResult.Statistics.Median / diffResult.Statistics.Median;
                    return (double.IsNaN(improvementXtimes) || double.IsInfinity(improvementXtimes))
                        ? Order(env) * 10.0
                        : Order(env) * Math.Min(improvementXtimes, 10.0);
                case EquivalenceTestConclusion.Slower:
                    double regressionXtimes = diffResult.Statistics.Median / baseResult.Statistics.Median;
                    return (double.IsNaN(regressionXtimes) || double.IsInfinity(regressionXtimes))
                        ? Order(env) * -10.0
                        : Order(env) * Math.Min(regressionXtimes, 10.0) * -1.0;
                default:
                    throw new NotSupportedException($"{conclusion} is not supported");
            }
        }

        private static int Order(HostEnvironmentInfo env)
        {
            const string windows = "windows", macos = "macos", linux = "linux";

            string os = env.OsVersion.StartsWith(windows, StringComparison.OrdinalIgnoreCase)
                ? windows
                : env.OsVersion.StartsWith(macos, StringComparison.OrdinalIgnoreCase) ? macos : linux;

            if (env.Architecture == "Arm64" && os == linux) return 1;
            else if (env.Architecture == "Arm64" && os == windows) return 2;
            else if (env.Architecture == "Arm64" && os == macos) return 3;
            else if (env.Architecture == "X64" && os == windows) return 4;
            else if (env.Architecture == "X64" && os == linux) return 5;
            else if (env.Architecture == "Arm" && os == windows) return 6;
            else if (env.Architecture == "Arm" && os == linux) return 7;
            else if (env.Architecture == "X86" && os == windows) return 8;
            else if (env.Architecture == "X64" && os == macos) return 9;
            else throw new NotSupportedException($"Config {env.Architecture} {env.OsVersion} was not recognized");
        }
    }
}
