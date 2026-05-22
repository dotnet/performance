using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Logging.StructuredLogger;
using StructuredLogViewer;
using Microsoft.Diagnostics.Tracing;
using Reporting;

namespace ScenarioMeasurement;

/// <summary>
/// Parses Android inner loop (build+deploy) target and task durations from a binary log file.
/// </summary>
public class AndroidInnerLoopParser : IParser
{
    public void EnableKernelProvider(ITraceSession kernel) { throw new NotImplementedException(); }
    public void EnableUserProviders(ITraceSession user) { throw new NotImplementedException(); }

    public IEnumerable<Counter> Parse(string binlogFile, string processName, IList<int> pids, string commandLine)
    {
        var buildDeployTimes = new List<double>();

        // Build tasks (compilation)
        var cscTimes = new List<double>();
        var xamlCTimes = new List<double>();
        var generateJavaStubsTimes = new List<double>();
        var linkAssembliesNoShrinkTimes = new List<double>();
        var d8Times = new List<double>();
        var javacTimes = new List<double>();
        var generateTypeMappingsTimes = new List<double>();
        var processAssembliesTimes = new List<double>();
        var generateJavaCallableWrappersTimes = new List<double>();
        var filterAssembliesTimes = new List<double>();
        var waitForAppDetectionTimes = new List<double>();
        var generateMainAndroidManifestTimes = new List<double>();
        var resolveSdksTimes = new List<double>();
        var generateNativeApplicationConfigSourcesTimes = new List<double>();

        // Deploy tasks
        var fastDeployTimes = new List<double>();
        var androidSignPackageTimes = new List<double>();
        var androidApkSignerTimes = new List<double>();
        var aapt2LinkTimes = new List<double>();

        // Build targets
        var coreCompileTargetTimes = new List<double>();
        var xamlCTargetTimes = new List<double>();
        var generateJavaStubsTargetTimes = new List<double>();
        var linkAssembliesNoShrinkTargetTimes = new List<double>();
        var compileToDalvikTargetTimes = new List<double>();
        var compileJavaTargetTimes = new List<double>();

        // Deploy targets
        var signTargetTimes = new List<double>();
        var uploadTargetTimes = new List<double>();
        var deployApkTargetTimes = new List<double>();
        var buildApkFastDevTargetTimes = new List<double>();

        if (File.Exists(binlogFile))
        {
            var build = BinaryLog.ReadBuild(binlogFile);
            BuildAnalyzer.AnalyzeBuild(build);

            foreach (var task in build.FindChildrenRecursive<Task>())
            {
                var name = task.Name;
                var s = task.Duration.TotalMilliseconds / 1000.0;

                if (name.Equals("Csc", StringComparison.OrdinalIgnoreCase))
                    cscTimes.Add(s);
                else if (name.Equals("XamlCTask", StringComparison.OrdinalIgnoreCase))
                    xamlCTimes.Add(s);
                else if (name.Equals("GenerateJavaStubs", StringComparison.OrdinalIgnoreCase))
                    generateJavaStubsTimes.Add(s);
                else if (name.Equals("LinkAssembliesNoShrink", StringComparison.OrdinalIgnoreCase))
                    linkAssembliesNoShrinkTimes.Add(s);
                else if (name.Equals("D8", StringComparison.OrdinalIgnoreCase))
                    d8Times.Add(s);
                else if (name.Equals("Javac", StringComparison.OrdinalIgnoreCase))
                    javacTimes.Add(s);
                else if (name.Equals("GenerateTypeMappings", StringComparison.OrdinalIgnoreCase))
                    generateTypeMappingsTimes.Add(s);
                else if (name.Equals("ProcessAssemblies", StringComparison.OrdinalIgnoreCase))
                    processAssembliesTimes.Add(s);
                else if (name.Equals("GenerateJavaCallableWrappers", StringComparison.OrdinalIgnoreCase))
                    generateJavaCallableWrappersTimes.Add(s);
                else if (name.Equals("FilterAssemblies", StringComparison.OrdinalIgnoreCase))
                    filterAssembliesTimes.Add(s);
                else if (name.Equals("WaitForAppDetection", StringComparison.OrdinalIgnoreCase))
                    waitForAppDetectionTimes.Add(s);
                else if (name.Equals("GenerateMainAndroidManifest", StringComparison.OrdinalIgnoreCase))
                    generateMainAndroidManifestTimes.Add(s);
                else if (name.Equals("ResolveSdks", StringComparison.OrdinalIgnoreCase))
                    resolveSdksTimes.Add(s);
                else if (name.Equals("GenerateNativeApplicationConfigSources", StringComparison.OrdinalIgnoreCase))
                    generateNativeApplicationConfigSourcesTimes.Add(s);
                else if (name.Equals("FastDeploy", StringComparison.OrdinalIgnoreCase))
                    fastDeployTimes.Add(s);
                else if (name.Equals("AndroidSignPackage", StringComparison.OrdinalIgnoreCase))
                    androidSignPackageTimes.Add(s);
                else if (name.Equals("AndroidApkSigner", StringComparison.OrdinalIgnoreCase))
                    androidApkSignerTimes.Add(s);
                else if (name.Equals("Aapt2Link", StringComparison.OrdinalIgnoreCase))
                    aapt2LinkTimes.Add(s);
            }

            foreach (var target in build.FindChildrenRecursive<Target>())
            {
                var name = target.Name;
                var s = target.Duration.TotalMilliseconds / 1000.0;

                if (name.Equals("CoreCompile", StringComparison.Ordinal))
                    coreCompileTargetTimes.Add(s);
                else if (name.Equals("XamlC", StringComparison.Ordinal))
                    xamlCTargetTimes.Add(s);
                else if (name.Equals("_GenerateJavaStubs", StringComparison.Ordinal))
                    generateJavaStubsTargetTimes.Add(s);
                else if (name.Equals("_LinkAssembliesNoShrink", StringComparison.Ordinal))
                    linkAssembliesNoShrinkTargetTimes.Add(s);
                else if (name.Equals("_CompileToDalvik", StringComparison.Ordinal))
                    compileToDalvikTargetTimes.Add(s);
                else if (name.Equals("_CompileJava", StringComparison.Ordinal))
                    compileJavaTargetTimes.Add(s);
                else if (name.Equals("_Sign", StringComparison.Ordinal))
                    signTargetTimes.Add(s);
                else if (name.Equals("_Upload", StringComparison.Ordinal))
                    uploadTargetTimes.Add(s);
                else if (name.Equals("_DeployApk", StringComparison.Ordinal))
                    deployApkTargetTimes.Add(s);
                else if (name.Equals("_BuildApkFastDev", StringComparison.Ordinal))
                    buildApkFastDevTargetTimes.Add(s);
            }

            buildDeployTimes.Add(build.Duration.TotalMilliseconds / 1000.0);
        }

        if (buildDeployTimes.Count > 0)
            yield return new Counter { Name = "Build+Deploy Time", MetricName = "s", DefaultCounter = true, TopCounter = true, Results = buildDeployTimes.ToArray() };

        foreach (var c in EmitCounters(
            ("Csc Task Time", cscTimes),
            ("XamlC Task Time", xamlCTimes),
            ("GenerateJavaStubs Task Time", generateJavaStubsTimes),
            ("LinkAssembliesNoShrink Task Time", linkAssembliesNoShrinkTimes),
            ("D8 Task Time", d8Times),
            ("Javac Task Time", javacTimes),
            ("GenerateTypeMappings Task Time", generateTypeMappingsTimes),
            ("ProcessAssemblies Task Time", processAssembliesTimes),
            ("GenerateJavaCallableWrappers Task Time", generateJavaCallableWrappersTimes),
            ("FilterAssemblies Task Time", filterAssembliesTimes),
            ("WaitForAppDetection Task Time", waitForAppDetectionTimes),
            ("GenerateMainAndroidManifest Task Time", generateMainAndroidManifestTimes),
            ("ResolveSdks Task Time", resolveSdksTimes),
            ("GenerateNativeApplicationConfigSources Task Time", generateNativeApplicationConfigSourcesTimes),
            ("CoreCompile Target Time", coreCompileTargetTimes),
            ("XamlC Target Time", xamlCTargetTimes),
            ("_GenerateJavaStubs Target Time", generateJavaStubsTargetTimes),
            ("_LinkAssembliesNoShrink Target Time", linkAssembliesNoShrinkTargetTimes),
            ("_CompileToDalvik Target Time", compileToDalvikTargetTimes),
            ("_CompileJava Target Time", compileJavaTargetTimes),
            ("_Sign Target Time", signTargetTimes),
            ("_Upload Target Time", uploadTargetTimes),
            ("_DeployApk Target Time", deployApkTargetTimes),
            ("_BuildApkFastDev Target Time", buildApkFastDevTargetTimes),
            ("FastDeploy Task Time", fastDeployTimes),
            ("AndroidSignPackage Task Time", androidSignPackageTimes),
            ("AndroidApkSigner Task Time", androidApkSignerTimes),
            ("Aapt2Link Task Time", aapt2LinkTimes)))
            yield return c;
    }

    private static IEnumerable<Counter> EmitCounters(params (string Name, List<double> Times)[] entries)
    {
        foreach (var (name, times) in entries)
        {
            if (times.Count > 0)
                yield return new Counter { Name = name, MetricName = "s", DefaultCounter = false, TopCounter = true, Results = times.ToArray() };
        }
    }
}
