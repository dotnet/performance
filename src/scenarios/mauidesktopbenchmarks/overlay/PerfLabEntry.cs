#nullable enable

using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MauiPerfLab;

internal static class EntryPoint
{
    public static async Task<int> Main(string[] args)
    {
        var argsList = args.ToList();
        argsList = CommandLineOptions.ParseAndRemoveStringsParameter(
            argsList,
            "--exclusion-filter",
            out List<string> exclusionFilter);

        string? artifactsPath = Environment.GetEnvironmentVariable("MAUI_PERFLAB_ARTIFACTS_PATH");
        if (string.IsNullOrWhiteSpace(artifactsPath))
        {
            throw new InvalidOperationException("MAUI_PERFLAB_ARTIFACTS_PATH must be set.");
        }

        IToolchain? toolchain =
            Environment.GetEnvironmentVariable("MAUI_PERFLAB_IN_PROCESS") == "1"
                ? InProcessEmitToolchain.Default
                : null;

        var config = RecommendedConfig.Create(
            new DirectoryInfo(artifactsPath),
            ImmutableHashSet<string>.Empty,
            exclusionFilterValue: exclusionFilter,
            toolchain: toolchain);

        var summaries = await BenchmarkSwitcher
            .FromAssembly(typeof(EntryPoint).Assembly)
            .RunAsync(argsList.ToArray(), config)
            .ConfigureAwait(false);

        return summaries.ToExitCode();
    }
}
