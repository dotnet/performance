using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Logging.StructuredLogger;
using StructuredLogViewer;
using Microsoft.Diagnostics.Tracing;
using Reporting;

namespace ScenarioMeasurement;

/// <summary>
/// Parses iOS inner loop (build+deploy) target and task durations from a binary log file.
/// </summary>
public class iOSInnerLoopParser : IParser
{
    public void EnableKernelProvider(ITraceSession kernel) { throw new NotImplementedException(); }
    public void EnableUserProviders(ITraceSession user) { throw new NotImplementedException(); }

    // Task name (case-insensitive) → counter display name
    private static readonly Dictionary<string, string> TrackedTasks = new(StringComparer.OrdinalIgnoreCase)
    {
        // Shared build tasks
        ["Csc"] = "Csc Task Time",
        ["XamlCTask"] = "XamlC Task Time",
        ["LinkAssembliesNoShrink"] = "LinkAssembliesNoShrink Task Time",
        ["FilterAssemblies"] = "FilterAssemblies Task Time",
        ["ResolveSdks"] = "ResolveSdks Task Time",
        ["ProcessAssemblies"] = "ProcessAssemblies Task Time",
        ["GenerateNativeApplicationConfigSources"] = "GenerateNativeApplicationConfigSources Task Time",
        // iOS-specific build tasks
        ["AOTCompile"] = "AOTCompile Task Time",
        ["MonoAOTCompiler"] = "MonoAOTCompiler Task Time",
        ["Codesign"] = "Codesign Task Time",
        ["CompileNativeFiles"] = "CompileNativeFiles Task Time",
        ["LinkNativeCode"] = "LinkNativeCode Task Time",
        ["GenerateBundleName"] = "GenerateBundleName Task Time",
        ["CreateAssetPack"] = "CreateAssetPack Task Time",
        ["ComputeCodesignInputs"] = "ComputeCodesignInputs Task Time",
        ["DetectSigningIdentity"] = "DetectSigningIdentity Task Time",
        ["CompileAppManifest"] = "CompileAppManifest Task Time",
        ["CompileEntitlements"] = "CompileEntitlements Task Time",
        ["CreateBindingResourcePackage"] = "CreateBindingResourcePackage Task Time",
        ["MTouch"] = "MTouch Task Time",
        ["ACTool"] = "ACTool Task Time",
        ["IBTool"] = "IBTool Task Time",
        ["DSymUtil"] = "DSymUtil Task Time",
    };

    // Target name (case-sensitive, matches MSBuild conventions) → counter display name
    private static readonly Dictionary<string, string> TrackedTargets = new(StringComparer.Ordinal)
    {
        ["CoreCompile"] = "CoreCompile Target Time",
        ["XamlC"] = "XamlC Target Time",
        ["_AOTCompile"] = "_AOTCompile Target Time",
        ["_CodesignAppBundle"] = "_CodesignAppBundle Target Time",
        ["_CompileToNative"] = "_CompileToNative Target Time",
        ["_CreateAppBundle"] = "_CreateAppBundle Target Time",
        ["_CopyResourcesToBundle"] = "_CopyResourcesToBundle Target Time",
        ["_GenerateBundleName"] = "_GenerateBundleName Target Time",
    };

    public IEnumerable<Counter> Parse(string binlogFile, string processName, IList<int> pids, string commandLine)
    {
        if (!File.Exists(binlogFile))
            yield break;

        var build = BinaryLog.ReadBuild(binlogFile);
        BuildAnalyzer.AnalyzeBuild(build);

        // Collect task durations
        var taskResults = TrackedTasks.ToDictionary(kv => kv.Key, _ => new List<double>(), StringComparer.OrdinalIgnoreCase);
        foreach (var task in build.FindChildrenRecursive<Task>())
        {
            if (taskResults.TryGetValue(task.Name, out var list))
                list.Add(task.Duration.TotalMilliseconds / 1000.0);
        }

        // Collect target durations
        var targetResults = TrackedTargets.ToDictionary(kv => kv.Key, _ => new List<double>(), StringComparer.Ordinal);
        foreach (var target in build.FindChildrenRecursive<Target>())
        {
            if (targetResults.TryGetValue(target.Name, out var list))
                list.Add(target.Duration.TotalMilliseconds / 1000.0);
        }

        // Overall build duration
        yield return new Counter { Name = "Build Time", MetricName = "s", DefaultCounter = true, TopCounter = true,
                                   Results = new[] { build.Duration.TotalMilliseconds / 1000.0 } };

        // Emit task counters
        foreach (var (taskName, counterName) in TrackedTasks)
        {
            var results = taskResults[taskName];
            if (results.Count > 0)
                yield return new Counter { Name = counterName, MetricName = "s", DefaultCounter = false, TopCounter = true, Results = results.ToArray() };
        }

        // Emit target counters
        foreach (var (targetName, counterName) in TrackedTargets)
        {
            var results = targetResults[targetName];
            if (results.Count > 0)
                yield return new Counter { Name = counterName, MetricName = "s", DefaultCounter = false, TopCounter = true, Results = results.ToArray() };
        }
    }
}
