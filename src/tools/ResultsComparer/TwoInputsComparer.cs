using DataTransferContracts;
using MarkdownLog;
using Perfolizer.Mathematics.SignificanceTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResultsComparer
{
    internal static class TwoInputsComparer
    {
        internal static void Compare(TwoInputsOptions args)
        {
            var notSame = GetNotSameResults(args).ToArray();

            if (!notSame.Any())
            {
                Console.WriteLine($"No differences found between the benchmark results with threshold {args.StatisticalTestThreshold}.");
                return;
            }

            PrintSummary(notSame);

            PrintTable(notSame, EquivalenceTestConclusion.Slower, args);
            PrintTable(notSame, EquivalenceTestConclusion.Faster, args);
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)> GetNotSameResults(TwoInputsOptions args)
        {
            foreach ((string id, Benchmark baseResult, Benchmark diffResult) in ReadResults(args)
                .Where(result => result.baseResult.Statistics != null && result.diffResult.Statistics != null)) // failures
            {
                var baseValues = baseResult.Statistics.OriginalValues.ToArray();
                var diffValues = diffResult.Statistics.OriginalValues.ToArray();

                var userTresholdResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, args.StatisticalTestThreshold);
                if (userTresholdResult.Conclusion == EquivalenceTestConclusion.Same)
                    continue;

                var noiseResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, args.NoiseThreshold);
                if (noiseResult.Conclusion == EquivalenceTestConclusion.Same)
                    continue;

                yield return (id, baseResult, diffResult, userTresholdResult.Conclusion);
            }
        }

        private static void PrintSummary((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)[] notSame)
        {
            var better = notSame.Where(result => result.conclusion == EquivalenceTestConclusion.Faster);
            var worse = notSame.Where(result => result.conclusion == EquivalenceTestConclusion.Slower);
            var betterCount = better.Count();
            var worseCount = worse.Count();

            // If the baseline doesn't have the same set of tests, you wind up with Infinity in the list of diffs.
            // Exclude them for purposes of geomean.
            worse = worse.Where(x => GetRatio(x) != double.PositiveInfinity);
            better = better.Where(x => GetRatio(x) != double.PositiveInfinity);

            Console.WriteLine("summary:");

            if (betterCount > 0)
            {
                var betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(GetRatio(better.First())), (x, y) => x + Math.Log10(GetRatio(y))) / better.Count());
                Console.WriteLine($"better: {betterCount}, geomean: {betterGeoMean:F3}");
            }

            if (worseCount > 0)
            {
                var worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(GetRatio(worse.First())), (x, y) => x + Math.Log10(GetRatio(y))) / worse.Count());
                Console.WriteLine($"worse: {worseCount}, geomean: {worseGeoMean:F3}");
            }

            Console.WriteLine($"total diff: {notSame.Count()}");
            Console.WriteLine();
        }

        private static void PrintTable((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)[] notSame, EquivalenceTestConclusion conclusion, TwoInputsOptions args)
        {
            var data = notSame
                .Where(result => result.conclusion == conclusion)
                .OrderByDescending(result => GetRatio(conclusion, result.baseResult, result.diffResult))
                .Take(args.TopCount ?? int.MaxValue)
                .Select(result => new
                {
                    Id = (result.id.Length <= 80 || args.FullId) ? result.id : result.id.Substring(0, 80),
                    DisplayValue = GetRatio(conclusion, result.baseResult, result.diffResult),
                    BaseMedian = result.baseResult.Statistics.Median,
                    DiffMedian = result.diffResult.Statistics.Median,
                    Modality = Helper.GetModalInfo(result.baseResult) ?? Helper.GetModalInfo(result.diffResult)
                })
                .ToArray();

            if (!data.Any())
            {
                Console.WriteLine($"No {conclusion} results for the provided threshold = {args.StatisticalTestThreshold} and noise filter = {args.NoiseThreshold}.");
                Console.WriteLine();
                return;
            }

            var table = data.ToMarkdownTable().WithHeaders(conclusion.ToString(), conclusion == EquivalenceTestConclusion.Faster ? "base/diff" : "diff/base", "Base Median (ns)", "Diff Median (ns)", "Modality");

            foreach (var line in table.ToMarkdown().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                Console.WriteLine($"| {line.TrimStart()}|"); // the table starts with \t and does not end with '|' and it looks bad so we fix it

            Console.WriteLine();
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult)> ReadResults(TwoInputsOptions args)
        {
            var baseFiles = Helper.GetFilesToParse(args.BasePath);
            var diffFiles = Helper.GetFilesToParse(args.DiffPath);

            if (!baseFiles.Any() || !diffFiles.Any())
                throw new ArgumentException($"Provided paths contained no {Helper.FullBdnJsonFileExtension} files.");

            var baseResults = baseFiles.Select(Helper.ReadFromFile);
            var diffResults = diffFiles.Select(Helper.ReadFromFile);

            var benchmarkIdToDiffResults = diffResults
                .SelectMany(result => result.Benchmarks)
                .Where(benchmarkResult => !args.Filters.Any() || args.Filters.Any(filter => filter.IsMatch(benchmarkResult.FullName)))
                .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult);

            return baseResults
                .SelectMany(result => result.Benchmarks)
                .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult) // we use ToDictionary to make sure the results have unique IDs
                .Where(baseResult => benchmarkIdToDiffResults.ContainsKey(baseResult.Key))
                .Select(baseResult => (baseResult.Key, baseResult.Value, benchmarkIdToDiffResults[baseResult.Key]));
        }

        private static double GetRatio((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion) item) => GetRatio(item.conclusion, item.baseResult, item.diffResult);

        private static double GetRatio(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
            => conclusion == EquivalenceTestConclusion.Faster
                ? baseResult.Statistics.Median / diffResult.Statistics.Median
                : diffResult.Statistics.Median / baseResult.Statistics.Median;
    }
}
