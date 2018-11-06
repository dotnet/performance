﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace RealWorld
{
    public class BenchmarkRunResult
    {
        public BenchmarkRunResult(Benchmark benchmark, BenchmarkConfiguration configuration)
        {
            Benchmark = benchmark;
            Configuration = configuration;
            IterationResults = new List<IterationResult>();
        }

        public Benchmark Benchmark { get; private set; }
        public BenchmarkConfiguration Configuration { get; private set; }
        public List<IterationResult> IterationResults { get; private set; }
    }

    public class IterationResult
    {
        public IterationResult()
        {
            Measurements = new Dictionary<Metric, double>();
        }
        public Dictionary<Metric, double> Measurements { get; private set; }
    }

    public struct Metric
    {
        public Metric(string name, string unit)
        {
            Name = name;
            Unit = unit;
        }
        public string Name { get; private set; }
        public string Unit { get; private set; }

        public static readonly Metric ElapsedTimeMilliseconds = new Metric("Duration", "ms");

        public override string ToString()
        {
            return $"{Name}({Unit})";
        }
    }
}
