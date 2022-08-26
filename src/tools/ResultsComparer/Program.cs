// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Threading;
using System.CommandLine;
using System.CommandLine.Parsing;
using Perfolizer.Mathematics.Thresholds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResultsComparer
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // we print a lot of numbers here and we want to make it always in invariant way
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Option<string> basePath = new Option<string>(
                new[] { "--base", "-b" }, "Path to the folder/file with base results.");
            Option<string> diffPath = new Option<string>(
                new[] { "--diff", "-d" }, "Path to the folder/file with diff results.");
            Option<string> threshold = new Option<string>(
                new[] { "--threshold", "-t" }, "Threshold for Statistical Test. Examples: 5%, 10ms, 100ns, 1s.");
            Option<string> noise = new Option<string>(
                new[] { "--noise", "-n" }, () => "0.3ns", "Noise threshold for Statistical Test. The difference for 1.0ns and 1.1ns is 10%, but it's just a noise. Examples: 0.5ns 1ns.");
            Option<int?> top = new Option<int?>(
                new[] { "--top" }, "Filter the diff to top/bottom N results. Optional.");
            Option<string[]> filters = new Option<string[]>(
                new[] { "--filter", "-f" }, "Filter the benchmarks by name using glob pattern(s).");
            Option<bool> fullId = new Option<bool>(
                new[] { "--full-id" }, "Display the full benchmark name id.");

            RootCommand rootCommand = new RootCommand
            {
                basePath, diffPath, threshold.AsRequired(), noise, top, filters, fullId
            };

            rootCommand.SetHandler<string, string, string, string, int?, string[], bool>(
                static (basePath, diffPath, threshold, noise, top, filters, fullId) =>
                {
                    if (TryParseThresholds(threshold, noise, out var testThreshold, out var noiseThreshold))
                    {
                        TwoInputsComparer.Compare(new TwoInputsOptions
                        {
                            BasePath = basePath,
                            DiffPath = diffPath,
                            StatisticalTestThreshold = testThreshold,
                            NoiseThreshold = noiseThreshold,
                            TopCount = top,
                            Filters = GetFilters(filters),
                            FullId = fullId
                        });
                    }
                },
                basePath, diffPath, threshold, noise, top, filters, fullId);

            Option<DirectoryInfo> input = new Option<DirectoryInfo>(
                new[] { "--input", "-i" }, "Path to the Input folder with BenchmarkDotNet .json files.");
            Option<string> basePattern = new Option<string>(
                new[] { "--base" }, "Pattern used to search for base results in Input folder. Example: net7.0-preview2");
            Option<string> diffPattern = new Option<string>(
                new[] { "--diff" }, "Pattern used to search for diff results in Input folder. Example: net7.0-preview3");
            Option<bool> printStats = new Option<bool>(
                new[] { "--stats" }, () => true, "Prints summary per Architecture, Namespace and Operating System.");
            Option<bool> ratioOnly = new Option<bool>(
                new[] { "--ratio-only" }, "Do not display the base and diff columns in the results.");

            Command matrixCommand = new Command("matrix", "Produces a matrix for all configurations found in given folder.")
            {
                input.AsRequired(), basePattern.AsRequired(), diffPattern.AsRequired(), threshold, noise, top, filters, printStats, ratioOnly
            };

            rootCommand.AddCommand(matrixCommand);

            matrixCommand.SetHandler<DirectoryInfo, string, string, string, string, int?, string[], bool, bool>(
                static (input, basePattern, diffPattern, threshold, noise, top, filters, printStats, ratioOnly) =>
                {
                    if (TryParseThresholds(threshold, noise, out var testThreshold, out var noiseThreshold)
                        && TryGetPaths(input, basePattern, diffPattern, out var basePaths, out var diffPaths))
                    {
                        MultipleInputsComparer.Compare(new MultipleInputsOptions
                        {
                            BasePattern = basePattern,
                            DiffPattern = diffPattern,
                            BasePaths = basePaths.ToArray(),
                            DiffPaths = diffPaths.ToArray(),
                            StatisticalTestThreshold = testThreshold,
                            NoiseThreshold = noiseThreshold,
                            TopCount = top,
                            Filters = GetFilters(filters),
                            PrintStats = printStats,
                            RatioOnly = ratioOnly
                        });
                    }
                }, input, basePattern, diffPattern, threshold, noise, top, filters, printStats, ratioOnly);

            Option<FileInfo> zip = new Option<FileInfo>(
                new[] { "--input", "-i" }, "Path to the compressed .zip file that contains results downloaded from SharePoint.");
            Option<DirectoryInfo> output = new Option<DirectoryInfo>(
                new[] { "--output" }, "Pattern to the output folder where decompressed results should be stored");

            Command decompressCommand = new Command("decompress", "Decompresses results from provided .zip file. Pre-configuration step for the matrix command.")
            {
                zip.AsRequired(), output.AsRequired()
            };

            decompressCommand.SetHandler<FileInfo, DirectoryInfo>(static (zip, output) => Data.Decompress(zip, output), zip, output);

            matrixCommand.AddCommand(decompressCommand);

            return rootCommand.Invoke(args);
        }

        private static bool TryParseThresholds(string test, string noise, out Threshold testThreshold, out Threshold noiseThreshold)
        {
            if (!Threshold.TryParse(test, out testThreshold))
            {
                Console.WriteLine($"Invalid Threshold '{test}'. Examples: 5%, 10ms, 100ns, 1s.");
                noiseThreshold = null;
                return false;
            }
            if (!Threshold.TryParse(noise, out noiseThreshold))
            {
                Console.WriteLine($"Invalid Noise Threshold '{noise}'. Examples: 0.3ns 1ns.");
                return false;
            }

            return true;
        }

        private static bool TryGetPaths(DirectoryInfo input, string basePattern, string diffPattern, out List<string> basePaths, out List<string> diffPaths)
        {
            basePaths = diffPaths = null;

            if (!input.Exists)
            {
                Console.WriteLine($"Provided Input folder '{input.FullName}' does NOT exist.");
                return false;
            }

            basePaths = new List<string>();
            diffPaths = new List<string>();

            foreach (var baseline in input.GetDirectories($"*{basePattern}*"))
            {
                var current = baseline.FullName.Replace(basePattern, diffPattern);
                if (Directory.Exists(current))
                {
                    basePaths.Add(baseline.FullName);
                    diffPaths.Add(current);
                }
                else
                {
                    Console.WriteLine($"Base results folder '{baseline.FullName}' has no corresponding diff results folder ('{current}').");
                }
            }

            if (!basePaths.Any())
            {
                Console.WriteLine($"Provided Input folder '{input.FullName}' does contain any subfolders that match the base pattern ('{basePattern}').");
                return false;
            }

            return true;
        }

        private static Regex[] GetFilters(string[] filters)
            =>  filters.Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();

        // https://stackoverflow.com/a/6907849/5852046 not perfect but should work for all we need
        private static string WildcardToRegex(string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";

        private static Option AsRequired(this Option option)
        {
            option.IsRequired = true;
            return option;
        }
    }
}
