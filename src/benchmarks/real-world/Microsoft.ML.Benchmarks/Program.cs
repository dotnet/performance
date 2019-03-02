// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Threading;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Extensions;

namespace Microsoft.ML.Benchmarks
{
    class Program
    {
        /// <summary>
        /// execute dotnet run -c Release and choose the benchmarks you want to run
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
            => BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BenchmarkDotNet.Artifacts")), 
                    mandatoryCategories: ImmutableHashSet.Create(Categories.MachineLearning)))
                .ToExitCode();

        internal static string GetInvariantCultureDataPath(string name)
        {
            // enforce Neutral Language as "en-us" because the input data files use dot as decimal separator (and it fails for cultures with ",")
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            return Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Input", name);
        }
    }
}
