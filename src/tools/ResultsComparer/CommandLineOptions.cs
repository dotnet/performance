// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace ResultsComparer
{
    public class CommandLineOptions
    {
        [Option("base", HelpText = "Path to the folder/file with base results.")]
        public string BasePath { get; set; }

        [Option("diff", HelpText = "Path to the folder/file with diff results.")]
        public string DiffPath { get; set; }

        [Option("merged", HelpText = "Path to the folder/file with results merged for multiple jobs in the same file.")]
        public string MergedPath { get; set; }

        [Option("treshold", Required = true, HelpText = "Threshold for Statistical Test. Examples: 5%, 10ms, 100ns, 1s.")]
        public string StatisticalTestThreshold { get; set; }

        [Option("top", HelpText = "Filter the diff to top/bottom N results. Optional.")]
        public int? TopCount { get; set; }

        [Usage(ApplicationAlias = "")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example(@"Compare the results stored in 'C:\results\win' (base) vs 'C:\results\unix' (diff) using 5% threshold.",
                    new CommandLineOptions { BasePath = @"C:\results\win", DiffPath = @"C:\results\unix", StatisticalTestThreshold = "5%" });
                yield return new Example(@"Compare the results stored in 'C:\results\21_vs_22' (multiple jobs per single benchmark run using 1ms threshold.",
                    new CommandLineOptions { MergedPath = @"C:\results\21_vs_22", StatisticalTestThreshold = "1ms" });
                yield return new Example(@"Compare the results stored in 'C:\results\win' (base) vs 'C:\results\unix' (diff) using 5% threshold and show only top/bottom 10 results.",
                    new CommandLineOptions { BasePath = @"C:\results\win", DiffPath = @"C:\results\unix", StatisticalTestThreshold = "5%", TopCount = 10 });
            }
        }
    }
}