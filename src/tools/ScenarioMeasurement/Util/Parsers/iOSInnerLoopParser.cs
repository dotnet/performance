using System;
using System.Collections.Generic;
using System.IO;
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

    public IEnumerable<Counter> Parse(string binlogFile, string processName, IList<int> pids, string commandLine)
    {
        var buildDeployTimes = new List<double>();

        // Build tasks (shared)
        var cscTimes = new List<double>();
        var xamlCTimes = new List<double>();
        var linkAssembliesNoShrinkTimes = new List<double>();
        var filterAssembliesTimes = new List<double>();
        var resolveSdksTimes = new List<double>();
        var processAssembliesTimes = new List<double>();
        var generateNativeApplicationConfigSourcesTimes = new List<double>();

        // iOS-specific build tasks
        var aotCompileTimes = new List<double>();
        var monoAOTCompilerTimes = new List<double>();
        var codesignTimes = new List<double>();
        var compileNativeFilesTimes = new List<double>();
        var linkNativeCodeTimes = new List<double>();
        var generateBundleNameTimes = new List<double>();
        var createAssetPackTimes = new List<double>();
        var computeCodesignInputsTimes = new List<double>();
        var detectSigningIdentityTimes = new List<double>();
        var compileAppManifestTimes = new List<double>();
        var compileEntitlementsTimes = new List<double>();
        var createBindingResourcePackageTimes = new List<double>();
        var mTouchTimes = new List<double>();
        var acToolTimes = new List<double>();
        var ibToolTimes = new List<double>();
        var dSymUtilTimes = new List<double>();

        // Build targets (shared)
        var coreCompileTargetTimes = new List<double>();
        var xamlCTargetTimes = new List<double>();

        // iOS-specific build targets
        var aotCompileTargetTimes = new List<double>();
        var codesignAppBundleTargetTimes = new List<double>();
        var compileToNativeTargetTimes = new List<double>();
        var createAppBundleTargetTimes = new List<double>();
        var copyResourcesToBundleTargetTimes = new List<double>();
        var generateBundleNameTargetTimes = new List<double>();

        if (File.Exists(binlogFile))
        {
            var build = BinaryLog.ReadBuild(binlogFile);
            BuildAnalyzer.AnalyzeBuild(build);

            foreach (var task in build.FindChildrenRecursive<Task>())
            {
                var name = task.Name;
                var s = task.Duration.TotalMilliseconds / 1000.0;

                // Shared build tasks
                if (name.Equals("Csc", StringComparison.OrdinalIgnoreCase))
                    cscTimes.Add(s);
                else if (name.Equals("XamlCTask", StringComparison.OrdinalIgnoreCase))
                    xamlCTimes.Add(s);
                else if (name.Equals("LinkAssembliesNoShrink", StringComparison.OrdinalIgnoreCase))
                    linkAssembliesNoShrinkTimes.Add(s);
                else if (name.Equals("FilterAssemblies", StringComparison.OrdinalIgnoreCase))
                    filterAssembliesTimes.Add(s);
                else if (name.Equals("ResolveSdks", StringComparison.OrdinalIgnoreCase))
                    resolveSdksTimes.Add(s);
                else if (name.Equals("ProcessAssemblies", StringComparison.OrdinalIgnoreCase))
                    processAssembliesTimes.Add(s);
                else if (name.Equals("GenerateNativeApplicationConfigSources", StringComparison.OrdinalIgnoreCase))
                    generateNativeApplicationConfigSourcesTimes.Add(s);
                // iOS-specific build tasks
                else if (name.Equals("AOTCompile", StringComparison.OrdinalIgnoreCase))
                    aotCompileTimes.Add(s);
                else if (name.Equals("MonoAOTCompiler", StringComparison.OrdinalIgnoreCase))
                    monoAOTCompilerTimes.Add(s);
                else if (name.Equals("Codesign", StringComparison.OrdinalIgnoreCase))
                    codesignTimes.Add(s);
                else if (name.Equals("CompileNativeFiles", StringComparison.OrdinalIgnoreCase))
                    compileNativeFilesTimes.Add(s);
                else if (name.Equals("LinkNativeCode", StringComparison.OrdinalIgnoreCase))
                    linkNativeCodeTimes.Add(s);
                else if (name.Equals("GenerateBundleName", StringComparison.OrdinalIgnoreCase))
                    generateBundleNameTimes.Add(s);
                else if (name.Equals("CreateAssetPack", StringComparison.OrdinalIgnoreCase))
                    createAssetPackTimes.Add(s);
                else if (name.Equals("ComputeCodesignInputs", StringComparison.OrdinalIgnoreCase))
                    computeCodesignInputsTimes.Add(s);
                else if (name.Equals("DetectSigningIdentity", StringComparison.OrdinalIgnoreCase))
                    detectSigningIdentityTimes.Add(s);
                else if (name.Equals("CompileAppManifest", StringComparison.OrdinalIgnoreCase))
                    compileAppManifestTimes.Add(s);
                else if (name.Equals("CompileEntitlements", StringComparison.OrdinalIgnoreCase))
                    compileEntitlementsTimes.Add(s);
                else if (name.Equals("CreateBindingResourcePackage", StringComparison.OrdinalIgnoreCase))
                    createBindingResourcePackageTimes.Add(s);
                else if (name.Equals("MTouch", StringComparison.OrdinalIgnoreCase))
                    mTouchTimes.Add(s);
                else if (name.Equals("ACTool", StringComparison.OrdinalIgnoreCase))
                    acToolTimes.Add(s);
                else if (name.Equals("IBTool", StringComparison.OrdinalIgnoreCase))
                    ibToolTimes.Add(s);
                else if (name.Equals("DSymUtil", StringComparison.OrdinalIgnoreCase))
                    dSymUtilTimes.Add(s);
            }

            foreach (var target in build.FindChildrenRecursive<Target>())
            {
                var name = target.Name;
                var s = target.Duration.TotalMilliseconds / 1000.0;

                // Shared build targets
                if (name.Equals("CoreCompile", StringComparison.Ordinal))
                    coreCompileTargetTimes.Add(s);
                else if (name.Equals("XamlC", StringComparison.Ordinal))
                    xamlCTargetTimes.Add(s);
                // iOS-specific build targets
                else if (name.Equals("_AOTCompile", StringComparison.Ordinal))
                    aotCompileTargetTimes.Add(s);
                else if (name.Equals("_CodesignAppBundle", StringComparison.Ordinal))
                    codesignAppBundleTargetTimes.Add(s);
                else if (name.Equals("_CompileToNative", StringComparison.Ordinal))
                    compileToNativeTargetTimes.Add(s);
                else if (name.Equals("_CreateAppBundle", StringComparison.Ordinal))
                    createAppBundleTargetTimes.Add(s);
                else if (name.Equals("_CopyResourcesToBundle", StringComparison.Ordinal))
                    copyResourcesToBundleTargetTimes.Add(s);
                else if (name.Equals("_GenerateBundleName", StringComparison.Ordinal))
                    generateBundleNameTargetTimes.Add(s);
            }

            buildDeployTimes.Add(build.Duration.TotalMilliseconds / 1000.0);
        }

        // Overall duration
        if (buildDeployTimes.Count > 0)
            yield return new Counter { Name = "Build Time", MetricName = "s", DefaultCounter = true, TopCounter = true, Results = buildDeployTimes.ToArray() };

        // Shared build task counters
        if (cscTimes.Count > 0)
            yield return new Counter { Name = "Csc Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = cscTimes.ToArray() };
        if (xamlCTimes.Count > 0)
            yield return new Counter { Name = "XamlC Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = xamlCTimes.ToArray() };
        if (linkAssembliesNoShrinkTimes.Count > 0)
            yield return new Counter { Name = "LinkAssembliesNoShrink Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = linkAssembliesNoShrinkTimes.ToArray() };
        if (filterAssembliesTimes.Count > 0)
            yield return new Counter { Name = "FilterAssemblies Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = filterAssembliesTimes.ToArray() };
        if (resolveSdksTimes.Count > 0)
            yield return new Counter { Name = "ResolveSdks Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = resolveSdksTimes.ToArray() };
        if (processAssembliesTimes.Count > 0)
            yield return new Counter { Name = "ProcessAssemblies Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = processAssembliesTimes.ToArray() };
        if (generateNativeApplicationConfigSourcesTimes.Count > 0)
            yield return new Counter { Name = "GenerateNativeApplicationConfigSources Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = generateNativeApplicationConfigSourcesTimes.ToArray() };

        // iOS-specific build task counters
        if (aotCompileTimes.Count > 0)
            yield return new Counter { Name = "AOTCompile Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = aotCompileTimes.ToArray() };
        if (monoAOTCompilerTimes.Count > 0)
            yield return new Counter { Name = "MonoAOTCompiler Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = monoAOTCompilerTimes.ToArray() };
        if (codesignTimes.Count > 0)
            yield return new Counter { Name = "Codesign Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = codesignTimes.ToArray() };
        if (compileNativeFilesTimes.Count > 0)
            yield return new Counter { Name = "CompileNativeFiles Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = compileNativeFilesTimes.ToArray() };
        if (linkNativeCodeTimes.Count > 0)
            yield return new Counter { Name = "LinkNativeCode Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = linkNativeCodeTimes.ToArray() };
        if (generateBundleNameTimes.Count > 0)
            yield return new Counter { Name = "GenerateBundleName Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = generateBundleNameTimes.ToArray() };
        if (createAssetPackTimes.Count > 0)
            yield return new Counter { Name = "CreateAssetPack Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = createAssetPackTimes.ToArray() };
        if (computeCodesignInputsTimes.Count > 0)
            yield return new Counter { Name = "ComputeCodesignInputs Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = computeCodesignInputsTimes.ToArray() };
        if (detectSigningIdentityTimes.Count > 0)
            yield return new Counter { Name = "DetectSigningIdentity Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = detectSigningIdentityTimes.ToArray() };
        if (compileAppManifestTimes.Count > 0)
            yield return new Counter { Name = "CompileAppManifest Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = compileAppManifestTimes.ToArray() };
        if (compileEntitlementsTimes.Count > 0)
            yield return new Counter { Name = "CompileEntitlements Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = compileEntitlementsTimes.ToArray() };
        if (createBindingResourcePackageTimes.Count > 0)
            yield return new Counter { Name = "CreateBindingResourcePackage Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = createBindingResourcePackageTimes.ToArray() };
        if (mTouchTimes.Count > 0)
            yield return new Counter { Name = "MTouch Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = mTouchTimes.ToArray() };
        if (acToolTimes.Count > 0)
            yield return new Counter { Name = "ACTool Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = acToolTimes.ToArray() };
        if (ibToolTimes.Count > 0)
            yield return new Counter { Name = "IBTool Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = ibToolTimes.ToArray() };
        if (dSymUtilTimes.Count > 0)
            yield return new Counter { Name = "DSymUtil Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = dSymUtilTimes.ToArray() };

        // Shared build target counters
        if (coreCompileTargetTimes.Count > 0)
            yield return new Counter { Name = "CoreCompile Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = coreCompileTargetTimes.ToArray() };
        if (xamlCTargetTimes.Count > 0)
            yield return new Counter { Name = "XamlC Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = xamlCTargetTimes.ToArray() };

        // iOS-specific build target counters
        if (aotCompileTargetTimes.Count > 0)
            yield return new Counter { Name = "_AOTCompile Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = aotCompileTargetTimes.ToArray() };
        if (codesignAppBundleTargetTimes.Count > 0)
            yield return new Counter { Name = "_CodesignAppBundle Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = codesignAppBundleTargetTimes.ToArray() };
        if (compileToNativeTargetTimes.Count > 0)
            yield return new Counter { Name = "_CompileToNative Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = compileToNativeTargetTimes.ToArray() };
        if (createAppBundleTargetTimes.Count > 0)
            yield return new Counter { Name = "_CreateAppBundle Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = createAppBundleTargetTimes.ToArray() };
        if (copyResourcesToBundleTargetTimes.Count > 0)
            yield return new Counter { Name = "_CopyResourcesToBundle Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = copyResourcesToBundleTargetTimes.ToArray() };
        if (generateBundleNameTargetTimes.Count > 0)
            yield return new Counter { Name = "_GenerateBundleName Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = generateBundleNameTargetTimes.ToArray() };
    }
}
