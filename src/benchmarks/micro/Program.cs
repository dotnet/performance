// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Running;
using System.IO;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks
{
    class Program
    {
        static int Main(string[] args)
        {
            var argsList = new List<string>(args);
            int? partitionCount;
            int? partitionIndex;

            // Parse and remove any additional parameters that we need that aren't part of BDN
            try {
                argsList = CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out partitionCount);
                argsList = CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out partitionIndex);

                CommandLineOptions.ValidatePartitionParameters(partitionCount, partitionIndex);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException: {0}", e.Message);
                return 1;
            }

            return BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(argsList.ToArray(), RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BenchmarkDotNet.Artifacts")), 
                    mandatoryCategories: ImmutableHashSet.Create(Categories.Libraries, Categories.Runtime, Categories.ThirdParty),
                    partitionCount: partitionCount,
                    partitionIndex: partitionIndex))
                .ToExitCode();
        }
    }

    [BenchmarkCategory(Categories.Libraries)]
    public class Broken
    {
        [Benchmark]
        public void Throwing() => throw new Exception("this should make the CI red");
    }
}