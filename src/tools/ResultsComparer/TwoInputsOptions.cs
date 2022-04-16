// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Perfolizer.Mathematics.Thresholds;
using System.Text.RegularExpressions;

namespace ResultsComparer
{
    public class TwoInputsOptions
    {
        public string BasePath { get; init; }
        public string DiffPath { get; init; }
        public Threshold StatisticalTestThreshold { get; init; }
        public Threshold NoiseThreshold { get; init; }
        public int? TopCount { get; init; }
        public Regex[] Filters { get; init; }
        public bool FullId { get; init; }
    }
}