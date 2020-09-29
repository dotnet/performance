// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GCPerf
{
    /// <summary>
    /// All things related to getting textual representation of perf data.
    /// </summary>
    public class TextualOutput
    {
        private static string metricsFormat = "|{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|";
        private static string metricsHeader = string.Format(metricsFormat,
            "Name",
            "Inc %",
            "Exc %",
            "Inc Samples",
            "Exc Samples");

        private static string metricsDiffFormat = "|{0,20}|{1,20}|{2,20}|{3,20}|{4,20}|{5,20}|{6,20}|{7,20}|{8,20}|";
        private static string metricsDiffHeader = string.Format(metricsDiffFormat,
            "Name",
            "Inc %", "Inc % Diff",
            "Exc %", "Exc % Diff",
            "Inc Samples", "Inc Samples Diff",
            "Exc Samples", "Exc Samples Diff");

        /// <summary>
        /// Get a table representing the list of metrics passed in 
        /// </summary>
        /// <param name="totalSamples">The total count of samples (used for calculating percentages)</param>
        /// <param name="metricsObjects">The set of metrics to include in the table</param>
        /// <param name="sortMetric">What to sort the table by</param>
        /// <returns>A string containing the table represented in plaintext</returns>
        public static string GetMetricsTable(int totalSamples, List<SampleMetrics> metricsObjects, SortMetric sortMetric)
        {
            StringBuilder tableStringBuilder = new StringBuilder();

            tableStringBuilder.AppendLine(metricsHeader);

            switch (sortMetric)
            {
                case SortMetric.Exclusive:
                    metricsObjects.Sort((a, b) => b.ExclusiveSamples.CompareTo(a.ExclusiveSamples));
                    break;
                case SortMetric.Inclusive:
                    metricsObjects.Sort((a, b) => b.InclusiveSamples.CompareTo(a.InclusiveSamples));
                    break;
            }

            foreach (var currMetrics in metricsObjects)
            {
                tableStringBuilder.AppendLine(GetMetricsString(totalSamples, currMetrics));
            }

            return tableStringBuilder.ToString();
        }

        // Like GetMetricsTable, but takes two lists of metrics and includes diffs.
        public static string GetMetricsDiffTable(
            int totalSamplesA,
            List<SampleMetrics> metricsObjectsA,
            int totalSamplesB,
            List<SampleMetrics> metricsObjectsB,
            SortMetric sortMetric)
        {
            StringBuilder tableStringBuilder = new StringBuilder();
            tableStringBuilder.AppendLine(metricsDiffHeader);

            switch (sortMetric)
            {
                case SortMetric.Exclusive:
                    metricsObjectsA.Sort((a, b) => b.ExclusiveSamples.CompareTo(a.ExclusiveSamples));
                    break;
                case SortMetric.Inclusive:
                    metricsObjectsA.Sort((a, b) => b.InclusiveSamples.CompareTo(a.InclusiveSamples));
                    break;
            }

            SampleMetrics emptyMetrics = new SampleMetrics();
            emptyMetrics.InclusiveSamples = 0;
            emptyMetrics.ExclusiveSamples = 0;

            HashSet<string> foundIdentifiers = new HashSet<string>();
            foreach (var currMetrics in metricsObjectsA)
            {
                bool foundInBaseline = false;
                foreach (var currBaselineMetrics in metricsObjectsB)
                {
                    if (currMetrics.Identifier == currBaselineMetrics.Identifier)
                    {
                        foundInBaseline = true;
                        foundIdentifiers.Add(currMetrics.Identifier);

                        tableStringBuilder.AppendLine(GetMetricsDiffString(totalSamplesA, currMetrics, totalSamplesB, currBaselineMetrics));
                        break;
                    }
                }

                if (!foundInBaseline)
                {
                    tableStringBuilder.AppendLine(GetMetricsDiffString(totalSamplesA, currMetrics, 1, emptyMetrics));
                }
            }

            // Also get everything in B that wasn't in A
            foreach (var currBaselineMetrics in metricsObjectsB)
            {
                if (!foundIdentifiers.Contains(currBaselineMetrics.Identifier))
                {
                    tableStringBuilder.AppendLine(GetMetricsDiffString(1, emptyMetrics, totalSamplesB, currBaselineMetrics));
                }
            }

            return tableStringBuilder.ToString();
        }

        public static string GetMetricsString(int totalSamples, SampleMetrics metrics)
        {
            double incPercent = 100.0 * ((double)metrics.InclusiveSamples / totalSamples);
            double excPercent = 100.0 * ((double)metrics.ExclusiveSamples / totalSamples);

            return string.Format(
                metricsFormat,
                TrimStringIfNeeded(metrics.Identifier, 20),
                string.Format("{0:0.00}", incPercent),
                string.Format("{0:0.00}", excPercent),
                metrics.InclusiveSamples,
                metrics.ExclusiveSamples);
        }

        public static string GetMetricsDiffString(int totalSamples, SampleMetrics metrics, int baselineTotalSamples, SampleMetrics baselineMetrics, bool percentageDiff = false)
        {
            double incPercent = 100.0 * ((double)metrics.InclusiveSamples / totalSamples);
            double incPercentBaseline = 100.0 * ((double)baselineMetrics.InclusiveSamples / baselineTotalSamples);

            double excPercent = 100.0 * ((double)metrics.ExclusiveSamples / totalSamples);
            double excPercentBaseline = 100.0 * ((double)baselineMetrics.ExclusiveSamples / baselineTotalSamples);

            Func<double, double, double> calculateDiff =
                percentageDiff ? new Func<double, double, double>((value, baseline) => (value - baseline) / baseline)
                               : new Func<double, double, double>((value, baseline) => (value - baseline));

            string diffPercentFormat = percentageDiff ? "+0.00%;-0.00%;0.00%" : "+0.00;-0.00;0.00";

            return string.Format(
                metricsDiffFormat,
                TrimStringIfNeeded(metrics.Identifier, 20),

                // Inc/Exc percentages and their diffs.
                string.Format("{0:0.00}", incPercent),
                calculateDiff(incPercent, incPercentBaseline).ToString(diffPercentFormat),

                string.Format("{0:0.00}", excPercent),
                calculateDiff(excPercent, excPercentBaseline).ToString(diffPercentFormat),

                // Inc/Exc counts and their diffs.
                metrics.InclusiveSamples,
                calculateDiff(metrics.InclusiveSamples, baselineMetrics.InclusiveSamples).ToString(),

                metrics.ExclusiveSamples,
                calculateDiff(metrics.ExclusiveSamples, baselineMetrics.ExclusiveSamples).ToString()
                );
        }

        private static string TrimStringIfNeeded(string toTrim, int maxChars)
        {
            if (toTrim.Length <= maxChars)
                return toTrim;

            return toTrim.Substring(0, maxChars - 3) + "...";
        }
    }
}
