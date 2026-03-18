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

        // Deploy-related tasks
        var fastDeployTimes = new List<double>();
        var androidSignPackageTimes = new List<double>();
        var androidApkSignerTimes = new List<double>();
        var aapt2LinkTimes = new List<double>();

        // Deploy-related targets
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

                if (name.Equals("FastDeploy", StringComparison.OrdinalIgnoreCase))
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

                if (name.Equals("_Sign", StringComparison.Ordinal))
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

        // Overall deploy duration
        if (publishTimes.Count > 0)
            yield return new Counter { Name = "Publish Time", MetricName = "s", DefaultCounter = true, TopCounter = true, Results = publishTimes.ToArray() };

        // Target-level counters (deploy phases)
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
