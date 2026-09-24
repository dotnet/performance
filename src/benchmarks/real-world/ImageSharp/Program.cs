// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace SixLabors.ImageSharp.Benchmarks
{
    public class Program
    {
        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The arguments to pass to the program.
        /// </param>
        // Use RunAsync (not Run) so BDN does not install its single-threaded
        // BenchmarkDotNetSynchronizationContext on the entrypoint thread.
        public static async Task<int> Main(string[] args)
        {
            var summaries = await BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .RunAsync(args, RecommendedConfig.Create(
                        artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BenchmarkDotNet.Artifacts")),
                        mandatoryCategories: ImmutableHashSet.Create(Categories.ImageSharp)))
                .ConfigureAwait(false);
            return summaries.ToExitCode();
        }
    }
}
