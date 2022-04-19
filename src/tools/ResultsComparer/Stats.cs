using DataTransferContracts;
using MarkdownLog;
using Perfolizer.Mathematics.SignificanceTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ResultsComparer
{
    internal class Stats
    {
        internal const EquivalenceTestConclusion Noise = (EquivalenceTestConclusion)123;
        private readonly Dictionary<string, PerConclusion> perArchitecture = new();
        private readonly Dictionary<string, PerConclusion> perNamespace = new();
        private readonly Dictionary<string, PerConclusion> perOS = new();
        private readonly PerConclusion totals = new();
        private bool printed = false;

        internal void Record(EquivalenceTestConclusion conclusion, HostEnvironmentInfo envInfo, Benchmark benchmark)
        {
            totals.Update(conclusion);

            Record(perArchitecture, envInfo.Architecture, conclusion);
            Record(perOS, GetSimplifiedOSName(envInfo.OsVersion), conclusion);

            if (!string.IsNullOrEmpty(benchmark.Namespace)) // some benchmarks have no namespace ;)
            {
                Record(perNamespace, benchmark.Namespace, conclusion);
            }

            static void Record(Dictionary<string, PerConclusion> dictionary, string key, EquivalenceTestConclusion conclusion)
            {
                if (!dictionary.TryGetValue(key, out var stats))
                {
                    dictionary[key] = stats = new PerConclusion();
                }
                stats.Update(conclusion);
            }
        }

        internal void Print()
        {
            if (printed)
            {
                return; // print them only once
            }
            printed = true;

            totals.Print();

            Print(perArchitecture, "Architecture");
            Print(perOS, "Operating System");
            Print(perNamespace, "Namespace");

            static void Print(Dictionary<string, PerConclusion> dictionary, string name)
            {
                Console.WriteLine($"## Statistics per {name}");
                Console.WriteLine();

                var data = dictionary.Select(pair => new
                {
                    Key = pair.Key,
                    Same = ((double)pair.Value.Same / pair.Value.Total).ToString("P2"),
                    Slower = ((double)pair.Value.Slower / pair.Value.Total).ToString("P2"),
                    Faster = ((double)pair.Value.Faster / pair.Value.Total).ToString("P2"),
                    Noise = ((double)pair.Value.Noise / pair.Value.Total).ToString("P2"),
                    Unknown = ((double)pair.Value.Unknown / pair.Value.Total).ToString("P2"),
                })
                .ToArray();

                var table = data.ToMarkdownTable().WithHeaders(name, "Same", "Slower", "Faster", "Noise", "Unknown");

                foreach (var line in table.ToMarkdown().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                    Console.WriteLine($"| {line.TrimStart()}|"); // the table starts with \t and does not end with '|' and it looks bad so we fix it

                Console.WriteLine();
            }
        }

        internal static string GetSimplifiedOSName(string text) => text.Split('(')[0];

        private class PerConclusion
        {
            internal long Total, Faster, Slower, Same, Unknown, Noise;

            internal void Update(EquivalenceTestConclusion conclusion)
            {
                Total++;

                switch (conclusion)
                {
                    case EquivalenceTestConclusion.Base:
                    case EquivalenceTestConclusion.Same:
                        Same++;
                        break;
                    case EquivalenceTestConclusion.Faster:
                        Faster++;
                        break;
                    case EquivalenceTestConclusion.Slower:
                        Slower++;
                        break;
                    case EquivalenceTestConclusion.Unknown:
                        Unknown++;
                        break;
                    case Stats.Noise:
                        Noise++;
                        break;
                    default:
                        throw new NotSupportedException($"Invalid conclusion! {conclusion}");
                }
            }

            internal void Print()
            {
                Console.WriteLine("## Statistics");
                Console.WriteLine();
                Console.WriteLine($"Total:   {Total}");
                Console.WriteLine($"Same:    {(double)Same / Total:P2}");
                Console.WriteLine($"Slower:  {(double)Slower / Total:P2}");
                Console.WriteLine($"Faster:  {(double)Faster / Total:P2}");
                Console.WriteLine($"Noise:   {(double)Noise / Total:P2}");
                Console.WriteLine($"Unknown: {(double)Unknown / Total:P2}");
                Console.WriteLine();
            }
        }
    }
}
