using BenchmarkDotNet.ConsoleArguments;
using CommandLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Extensions
{
    public class PerfLabCommandLineOptions : CommandLineOptions
    {
        [Option("partition-count", Required = false, HelpText = "Number of partitions that the run has been split into")]
        public int? PartitionCount { get; set; }

        [Option("partition-index", Required = false, HelpText = "Index of the partition that this run is for (0-based)")]
        public int? PartitionIndex { get; set; }

        [Option("exclusion-filter", Required = false, HelpText = "Glob patterns to exclude from being run")]
        public IEnumerable<string>? ExclusionFilters { get; set; }

        [Option("category-exclusion-filter", Required = false, HelpText = "Categories to exclude from being run")]
        public IEnumerable<string>? CategoryExclusionFilters { get; set; }

        [Option("disasm-diff", Required = false, Default = false, HelpText = "Enable diffable disassembly output")]
        public bool GetDiffableDisasm { get; set; }

        [Option("manifest", Required = false, HelpText = "Path to the json manifest file that contains the list of benchmarks to run")]
        public FileInfo? ManifestFile { get; set; }

        public BenchmarkManifest? Manifest { get; set; }

        public static bool TryParse(string[] args, out PerfLabCommandLineOptions? options, out string[]? bdnOnlyArgs)
        {
            using var parser = new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.EnableDashDash = true;
                settings.IgnoreUnknownArguments = false;
            });

            var result = parser.ParseArguments<PerfLabCommandLineOptions>(args);
            if (result is not Parsed<PerfLabCommandLineOptions> parsed || !ValidateOptions(parsed.Value))
            {
                options = null;
                bdnOnlyArgs = null;
                return false;
            }

            options = parsed.Value;

            // Parse again, set the custom options to null, then 'unparse' to get the BDN-only arguments.
            var bdnResult = parser.ParseArguments<PerfLabCommandLineOptions>(args).Value;

            // Iterate through props using reflection
            var customProps = typeof(PerfLabCommandLineOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in customProps)
            {
                if (prop.CanWrite)
                    prop.SetValue(bdnResult, null);
            }

            bdnOnlyArgs = parser.FormatCommandLineArgs(bdnResult, o => o.SkipDefault = true);
            return true;
        }

        public static bool ValidateOptions(PerfLabCommandLineOptions options)
        {
            if (options.PartitionIndex is int index)
            {
                if (options.PartitionCount is not int count)
                {
                    Console.Error.WriteLine("If --partition-index is specified, --partition-count must also be specified");
                    return false;
                }

                if (count < 2)
                {
                    Console.Error.WriteLine("When specified, value of --partition-count must be greater than 1");
                    return false;
                }
                else if (index >= count)
                {
                    Console.Error.WriteLine("Value of --partition-index must be less than --partition-count");
                    return false;
                }
                else if (index < 0)
                {
                    Console.Error.WriteLine("Value of --partition-index must be greater than or equal to 0");
                    return false;
                }
            }
            else if (options.PartitionCount is int)
            {
                Console.Error.WriteLine("If --partition-count is specified, --partition-index must also be specified");
                return false;
            }

            if (options.ManifestFile is FileInfo manifest)
            {
                try
                {
                    var fileContent = File.ReadAllText(manifest.FullName);
                    options.Manifest = JsonConvert.DeserializeObject<BenchmarkManifest>(fileContent)!;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to read manifest file '{manifest.FullName}': {ex.Message}");
                    return false;
                }
            }

            return true;
        }
    }
}