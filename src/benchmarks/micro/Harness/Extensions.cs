// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace MicroBenchmarks
{
    internal static class Extensions
    {
        internal static int ToExitCode(this IEnumerable<Summary> summaries)
        {
            // an empty summary means that initial filtering and validation did not allow to run
            if (!summaries.Any()) 
                return 1;

            // if anything has failed, it's an error
            if (summaries.Any(summary => summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.BuildResult.IsBuildSuccess || !report.ExecuteResults.Any())))
                return 1;

            return 0;
        }
    }
}