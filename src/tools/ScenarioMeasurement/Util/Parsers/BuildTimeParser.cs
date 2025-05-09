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
/// Parses the build time from a binary log file.
/// </summary>
public class BuildTimeParser : IParser
{
    public void EnableKernelProvider(ITraceSession kernel)
    {
        throw new NotImplementedException();
    }

    public void EnableUserProviders(ITraceSession user)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Counter> Parse(string binlogFile, string processName, IList<int> pids, string commandLine)
    {
        var illinkTimes = new List<double>();
        var monoaotcompilerTimes = new List<double>();
        var appleappbuilderTimes = new List<double>();
        var androidappbuilderTimes = new List<double>();
        var publishTimes = new List<double>();

        if (File.Exists(binlogFile))
        {
            var build = BinaryLog.ReadBuild(binlogFile);
            BuildAnalyzer.AnalyzeBuild(build);

            foreach (var task in build.FindChildrenRecursive<Task>())
            {
                var name = task.Name;
                var s = task.Duration.TotalMilliseconds / 1000.0;

                if (name.Equals("ILLink", StringComparison.OrdinalIgnoreCase))
                {
                    illinkTimes.Add(s);
                }
                else if (name.Equals("MonoAOTCompiler", StringComparison.OrdinalIgnoreCase))
                {
                    monoaotcompilerTimes.Add(s);
                }
                else if (name.Equals("AppleAppBuilderTask", StringComparison.OrdinalIgnoreCase))
                {
                    appleappbuilderTimes.Add(s);
                }
                else if (name.Equals("AndroidAppBuilderTask", StringComparison.OrdinalIgnoreCase))
                {
                    androidappbuilderTimes.Add(s);
                }
            }

            publishTimes.Add(build.Duration.TotalMilliseconds / 1000.0);
        }


        if (illinkTimes.Count > 0)
            yield return new Counter { Name = "ILLink Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = illinkTimes.ToArray() };
        if (monoaotcompilerTimes.Count > 0)
            yield return new Counter { Name = "MonoAOTCompiler Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = monoaotcompilerTimes.ToArray() };
        if (appleappbuilderTimes.Count > 0)
            yield return new Counter { Name = "AppleAppBuilderTask Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = appleappbuilderTimes.ToArray() };
        if (androidappbuilderTimes.Count > 0)
            yield return new Counter { Name = "AndroidAppBuilderTask Time", MetricName = "s", DefaultCounter = false, TopCounter = true, Results = androidappbuilderTimes.ToArray() };
        if (publishTimes.Count > 0)
            yield return new Counter { Name = "Publish Time", MetricName = "s", DefaultCounter = true, TopCounter = true, Results = publishTimes.ToArray() };
    }
}
