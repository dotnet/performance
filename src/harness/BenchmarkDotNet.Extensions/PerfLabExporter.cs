// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Reporting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Extensions
{
    // Implements IExporter directly (not ExporterBase) because PerfLabExporter writes
    // a file with a custom name pattern ("{type}-perf-lab-report.json") via
    // File.WriteAllTextAsync and manages the file lifecycle itself, rather than having
    // ExporterBase open and hand us a writer for a default-named file.
    public class PerfLabExporter : IExporter
    {
        private const string FileExtension = "json";
        private const string FileCaption = "perf-lab-report";

        public string Name => nameof(PerfLabExporter);

        public async ValueTask ExportAsync(Summary summary, ILogger logger, CancellationToken cancellationToken)
        {
            string? jsonOutput = BuildJson(summary);
            if (jsonOutput is null)
                return;

            string filePath = GetArtifactFullName(summary);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    string uniqueString = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    string altPath = $"{Path.Combine(summary.ResultsDirectoryPath, GetFileName(summary))}-{FileCaption}-{uniqueString}.{FileExtension}";
                    logger.WriteLineError($"Could not overwrite file {filePath}. Exporting to {altPath}");
                    filePath = altPath;
                }
            }

            await File.WriteAllTextAsync(filePath, jsonOutput, cancellationToken).ConfigureAwait(false);
            logger.WriteLineInfo($"  {filePath}");
        }

        private string GetArtifactFullName(Summary summary)
            => $"{Path.Combine(summary.ResultsDirectoryPath, GetFileName(summary))}-{FileCaption}.{FileExtension}";

        private static string GetFileName(Summary summary)
        {
            var targets = summary.BenchmarksCases.Select(b => b.Descriptor.Type).Distinct().ToArray();
            if (targets.Length == 1)
                return FolderNameHelper.ToFolderName(targets.Single());
            return summary.Title;
        }

        private static string? BuildJson(Summary summary)
        {
            var reporter = new Reporter();

            // Add BDN version to build AdditionalData when running in lab
            var bdnVersion = summary.HostEnvironmentInfo.BenchmarkDotNetVersion;
            if (reporter.Build != null && !string.IsNullOrEmpty(bdnVersion))
            {
                reporter.Build.AdditionalData["BenchmarkDotNetVersion"] = bdnVersion;
            }

            var hasCriticalErrors = summary.HasCriticalValidationErrors;

            DisassemblyDiagnoser? disassemblyDiagnoser = summary.Reports
                .FirstOrDefault()? // disassembler was either enabled for all or none of them (so we use the first one)
                .BenchmarkCase.Config.GetDiagnosers().OfType<DisassemblyDiagnoser>().FirstOrDefault();

            foreach (var report in summary.Reports)
            {
                // Skip individual reports with errors
                if (report.HasAnyErrors())
                {
                    continue;
                }

                var test = new Test();
                test.Name = FullNameProvider.GetBenchmarkName(report.BenchmarkCase);
                test.Categories = report.BenchmarkCase.Descriptor.Categories;

                if (hasCriticalErrors)
                {
                    test.AdditionalData["criticalErrors"] = "true";
                }

                var results = from result in report.AllMeasurements
                              where result.IterationMode == Engines.IterationMode.Workload && result.IterationStage == Engines.IterationStage.Result
                              orderby result.LaunchIndex, result.IterationIndex
                              select new { result.Nanoseconds, result.Operations };

                var overheadResults = from result in report.AllMeasurements
                                      where result.IsOverhead() && result.IterationStage != Engines.IterationStage.Jitting
                                      orderby result.LaunchIndex, result.IterationIndex
                                      select new { result.Nanoseconds, result.Operations };

                test.Counters.Add(new Counter
                {
                    Name = "Duration of single invocation",
                    TopCounter = true,
                    DefaultCounter = true,
                    HigherIsBetter = false,
                    MetricName = "ns",
                    Results = (from result in results
                               select result.Nanoseconds / result.Operations).ToList()
                });
                test.Counters.Add(new Counter
                {
                    Name = "Overhead invocation",
                    TopCounter = false,
                    DefaultCounter = false,
                    HigherIsBetter = false,
                    MetricName = "ns",
                    Results = (from result in overheadResults
                               select result.Nanoseconds / result.Operations).ToList()
                });
                test.Counters.Add(new Counter
                {
                    Name = "Duration",
                    TopCounter = false,
                    DefaultCounter = false,
                    HigherIsBetter = false,
                    MetricName = "ns",
                    Results = (from result in results
                               select result.Nanoseconds).ToList()
                });

                test.Counters.Add(new Counter
                {
                    Name = "Operations",
                    TopCounter = false,
                    DefaultCounter = false,
                    HigherIsBetter = true,
                    MetricName = "Count",
                    Results = (from result in results
                               select (double)result.Operations).ToList()
                });

                foreach (var metric in report.Metrics.Keys)
                {
                    var m = report.Metrics[metric];
                    test.Counters.Add(new Counter
                    {
                        Name = m.Descriptor.DisplayName,
                        TopCounter = false,
                        DefaultCounter = false,
                        HigherIsBetter = m.Descriptor.TheGreaterTheBetter,
                        MetricName = m.Descriptor.Unit,
                        Results = new[] { m.Value }
                    });
                }

                if (disassemblyDiagnoser != null && disassemblyDiagnoser.Results.TryGetValue(report.BenchmarkCase, out var disassemblyResult))
                {
                    string disassembly = DiffableDisassemblyExporter.BuildDisassemblyString(disassemblyResult, disassemblyDiagnoser.Config);
                    test.AdditionalData["disasm"] = disassembly;
                }

                reporter.AddTest(test);
            }

            return reporter.GetJson();
        }
    }
}
