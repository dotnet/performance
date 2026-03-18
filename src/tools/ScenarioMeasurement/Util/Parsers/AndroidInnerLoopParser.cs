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
        var publishTimes = new List<double>();

        // Build tasks (compilation)
        var cscTimes = new List<double>();
        var xamlCTimes = new List<double>();
        var generateJavaStubsTimes = new List<double>();
        var linkAssembliesNoShrinkTimes = new List<double>();
        var d8Times = new List<double>();
        var javacTimes = new List<double>();

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

            publishTimes.Add(build.Duration.TotalMilliseconds / 1000.0);
        }

        // Overall duration
        if (publishTimes.Count > 0)
            yield return new Counter { Name = "Publish Time", MetricName = "s", DefaultCounter = true, TopCounter = true, Results = publishTimes.ToArray() };

        // Build task counters
        if (cscTimes.Count > 0)
            yield return new Counter { Name = "Csc Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = cscTimes.ToArray() };
        if (xamlCTimes.Count > 0)
            yield return new Counter { Name = "XamlC Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = xamlCTimes.ToArray() };
        if (generateJavaStubsTimes.Count > 0)
            yield return new Counter { Name = "GenerateJavaStubs Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = generateJavaStubsTimes.ToArray() };
        if (linkAssembliesNoShrinkTimes.Count > 0)
            yield return new Counter { Name = "LinkAssembliesNoShrink Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = linkAssembliesNoShrinkTimes.ToArray() };
        if (d8Times.Count > 0)
            yield return new Counter { Name = "D8 Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = d8Times.ToArray() };
        if (javacTimes.Count > 0)
            yield return new Counter { Name = "Javac Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = javacTimes.ToArray() };

        // Build target counters
        if (coreCompileTargetTimes.Count > 0)
            yield return new Counter { Name = "CoreCompile Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = coreCompileTargetTimes.ToArray() };
        if (xamlCTargetTimes.Count > 0)
            yield return new Counter { Name = "XamlC Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = xamlCTargetTimes.ToArray() };
        if (generateJavaStubsTargetTimes.Count > 0)
            yield return new Counter { Name = "_GenerateJavaStubs Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = generateJavaStubsTargetTimes.ToArray() };
        if (linkAssembliesNoShrinkTargetTimes.Count > 0)
            yield return new Counter { Name = "_LinkAssembliesNoShrink Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = linkAssembliesNoShrinkTargetTimes.ToArray() };
        if (compileToDalvikTargetTimes.Count > 0)
            yield return new Counter { Name = "_CompileToDalvik Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = compileToDalvikTargetTimes.ToArray() };
        if (compileJavaTargetTimes.Count > 0)
            yield return new Counter { Name = "_CompileJava Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = compileJavaTargetTimes.ToArray() };

        // Deploy target counters
        if (signTargetTimes.Count > 0)
            yield return new Counter { Name = "_Sign Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = signTargetTimes.ToArray() };
        if (uploadTargetTimes.Count > 0)
            yield return new Counter { Name = "_Upload Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = uploadTargetTimes.ToArray() };
        if (deployApkTargetTimes.Count > 0)
            yield return new Counter { Name = "_DeployApk Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = deployApkTargetTimes.ToArray() };
        if (buildApkFastDevTargetTimes.Count > 0)
            yield return new Counter { Name = "_BuildApkFastDev Target Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = buildApkFastDevTargetTimes.ToArray() };

        // Task-level counters (granular)
        if (fastDeployTimes.Count > 0)
            yield return new Counter { Name = "FastDeploy Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = fastDeployTimes.ToArray() };
        if (androidSignPackageTimes.Count > 0)
            yield return new Counter { Name = "AndroidSignPackage Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = androidSignPackageTimes.ToArray() };
        if (androidApkSignerTimes.Count > 0)
            yield return new Counter { Name = "AndroidApkSigner Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = androidApkSignerTimes.ToArray() };
        if (aapt2LinkTimes.Count > 0)
            yield return new Counter { Name = "Aapt2Link Task Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = aapt2LinkTimes.ToArray() };
    }
}
