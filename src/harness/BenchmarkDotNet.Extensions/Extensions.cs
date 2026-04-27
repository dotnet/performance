// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Extensions
{
    public static class SummaryExtensions
    {
        public static int ToExitCode(this IEnumerable<Summary> summaries)
        {
            // an empty summary means that initial filtering and validation did not allow to run
            if (!summaries.Any()) 
                return 1;

            // if anything has failed, it's an error
            if (summaries.Any(summary => summary.HasAnyErrors()))
                return 1;

            return 0;
        }

        public static bool HasAnyErrors(this Summary summary)
        {
            return summary.HasCriticalValidationErrors || summary.Reports.Any(report => report.HasAnyErrors());
        }

        public static bool HasAnyErrors(this BenchmarkReport report)
        {
            return !report.BuildResult.IsBuildSuccess || !report.AllMeasurements.Any();
        }
    }
}