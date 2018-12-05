// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Mathematics.StatisticalTesting;
using CommandLine;
using DataTransferContracts;
using Newtonsoft.Json;

namespace ResultsComparer
{
    public class Program
    {
        private const string FullBdnJsonFileExtension = "full.json";

        private static readonly Threshold NoiseThreshold = Threshold.Create(ThresholdUnit.Nanoseconds, 0.3);

        public static void Main(string[] args) => Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(Compare);

        private static void Compare(CommandLineOptions args)
        {
            if (!Threshold.TryParse(args.StatisticalTestThreshold, out var userProvidedThreshold))
            {
                Console.WriteLine($"Invalid Threshold {args.StatisticalTestThreshold}. Examples: 5%, 10ms, 100ns, 1s.");
                return;
            }

            var notSame = GetNotSameResults(args, userProvidedThreshold).ToArray();
            var takeCount = args.TopCount.HasValue ? args.TopCount.Value : int.MaxValue;

            Console.WriteLine("SLOWER:");
            foreach (var result in notSame.Where(result => result.conclusion == EquivalenceTestConclusion.Slower).OrderBy(pair => pair.ratio).Take(takeCount))
                PrintResult(result);

            Console.WriteLine();
            Console.WriteLine("FASTER:");
            foreach (var result in notSame.Where(result => result.conclusion == EquivalenceTestConclusion.Faster).OrderByDescending(pair => pair.ratio).Take(takeCount))
                PrintResult(result);
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion, double ratio)> GetNotSameResults(CommandLineOptions args, Threshold userProvidedThreshold)
        {
            foreach (var pair in ReadResults(args)
                .Where(result => result.baseResult.Statistics != null && result.diffResult.Statistics != null)) // failures
            {
                var baseValues = pair.baseResult.GetOriginalValues();
                var diffValues = pair.diffResult.GetOriginalValues();

                var userTresholdResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, userProvidedThreshold);
                if (userTresholdResult.Conclusion == EquivalenceTestConclusion.Same)
                    continue;

                // the difference for 1.0ns and 1.1ns is 10%, but it's just a noise. So we test against 0.3ns to filter out the nano-benchmarks noise
                var noiseResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, NoiseThreshold);
                if (noiseResult.Conclusion == EquivalenceTestConclusion.Same)
                    continue;

                var ratio = (1.0 - pair.diffResult.Statistics.Median / pair.baseResult.Statistics.Median);

                yield return (pair.id, pair.baseResult, pair.diffResult, userTresholdResult.Conclusion, ratio);
            }
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult)> ReadResults(CommandLineOptions args)
        {
            // this is the case where BDN run one benchmark for multiple jobs, for example for two runtimes using -r netcoreapp2.1 netcoreapp2.2
            if (!string.IsNullOrEmpty(args.MergedPath))
            {
                return GetFilesToParse(args.MergedPath)
                    .Select(ReadFromFile)
                    .SelectMany(result => result.Benchmarks)
                    .GroupBy(result => result.FullName)
                        .SelectMany(sameKey => sameKey
                            .Select(group => (group.FullName, sameKey.First(), group)) // first is always the base
                            .Skip(1)); // we skip the first entry in the group, we want to do 1st vs 2nd, 1st vs 3rd etc..
            }
            else if(!string.IsNullOrEmpty(args.BasePath) && !string.IsNullOrEmpty(args.DiffPath))
            {
                var baseFiles = GetFilesToParse(args.BasePath);
                var diffFiles = GetFilesToParse(args.DiffPath);

                if (!baseFiles.Any() || !diffFiles.Any())
                    throw new ArgumentException($"Provided paths contained no {FullBdnJsonFileExtension} files.");

                var baseResults = baseFiles.Select(ReadFromFile);
                var diffResults = diffFiles.Select(ReadFromFile);

                var benchmarkIdToDiffResults = diffResults.SelectMany(result => result.Benchmarks).ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult);

                return baseResults
                    .SelectMany(result => result.Benchmarks)
                    .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult) // we use ToDictionary to make sure the results have unique IDs
                    .Where(baseResult => benchmarkIdToDiffResults.ContainsKey(baseResult.Key))
                    .Select(baseResult => (baseResult.Key, baseResult.Value, benchmarkIdToDiffResults[baseResult.Key]));
            }
            else
            {
                Console.WriteLine("Invalid parameters, use --help to find out how to use this app");

                return Array.Empty<(string id, Benchmark baseResult, Benchmark diffResult)>();
            }
        }

        private static string[] GetFilesToParse(string path)
        {
            if (Directory.Exists(path))
                return Directory.GetFiles(path, $"*{FullBdnJsonFileExtension}", SearchOption.AllDirectories);
            else if (File.Exists(path) || !path.EndsWith(FullBdnJsonFileExtension))
                return new[] { path };
            else
                throw new FileNotFoundException($"Provided path does NOT exist or is not a {path} file", path);
        }

        private static void PrintResult((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion, double ratio) result)
            => Console.WriteLine($"{result.ratio:0.00%} {result.id} {GetModalInfo(result.baseResult) ?? GetModalInfo(result.diffResult)}");

        // code and magic values taken from BenchmarkDotNet.Analysers.MultimodalDistributionAnalyzer
        // See http://www.brendangregg.com/FrequencyTrails/modes.html
        private static string GetModalInfo(Benchmark benchmark)
        {
            if (benchmark.Statistics.N < 12) // not enough data to tell
                return null;

            double mValue = MathHelper.CalculateMValue(new BenchmarkDotNet.Mathematics.Statistics(benchmark.GetOriginalValues()));
            if (mValue > 4.2)
                return "[multimodal]";
            else if (mValue > 3.2)
                return "[bimodal]";
            else if (mValue > 2.8)
                return "[can have several modes]";

            return null;
        }

        private static BdnResult ReadFromFile(string resultFilePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<BdnResult>(File.ReadAllText(resultFilePath));
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine($"Exception while reading the {resultFilePath} file.");

                throw;
            }
        }
    }
}