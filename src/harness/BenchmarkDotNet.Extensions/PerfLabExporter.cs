// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Reporting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    internal class PerfLabExporter : ExporterBase
    {
        protected override string FileExtension => "json";
        protected override string FileCaption => "perf-lab-report";
        public PerfLabExporter()
        {
        }
        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var reporter = Reporter.CreateReporter();
            if (!reporter.InLab) // not running in the perf lab
                return;

            foreach (var report in summary.Reports)
            {
                var test = new Test();
                test.Name = FullNameProvider.GetBenchmarkName(report.BenchmarkCase);
                test.Categories = report.BenchmarkCase.Descriptor.Categories;
                var results = from result in report.AllMeasurements
                              where result.IterationMode == Engines.IterationMode.Workload && result.IterationStage == Engines.IterationStage.Result
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
                    Name = "Duration",
                    TopCounter = false,
                    DefaultCounter = false,
                    HigherIsBetter = false,
                    MetricName = "ms",
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
                               select  (double)result.Operations).ToList()
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

                reporter.AddTest(test);
            }

            logger.WriteLine(reporter.GetJson());
        }
    }
}