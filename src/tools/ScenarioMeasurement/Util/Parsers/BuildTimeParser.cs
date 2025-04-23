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
        var publishTimes = new List<double>();

        if (File.Exists(binlogFile))
        {
            var build = BinaryLog.ReadBuild(binlogFile);
            BuildAnalyzer.AnalyzeBuild(build);

            foreach (var task in build.FindChildrenRecursive<Task>())
            {
                var name = task.Name;
                var ms = task.Duration.TotalMilliseconds;

                if (name.Equals("ILLink", StringComparison.OrdinalIgnoreCase))
                {
                    illinkTimes.Add(ms);
                }
                else if (name.Equals("MonoAOTCompiler", StringComparison.OrdinalIgnoreCase))
                {
                    monoaotcompilerTimes.Add(ms);
                }
                else if (name.Equals("AppleAppBuilderTask", StringComparison.OrdinalIgnoreCase))
                {
                    appleappbuilderTimes.Add(ms);
                }
            }

            publishTimes.Add(build.Duration.TotalMilliseconds);
        }


        if (illinkTimes.Count > 0)
            yield return new Counter { Name = "ILLink Time", MetricName = "ms", DefaultCounter = false, TopCounter = true, Results = illinkTimes.ToArray() };
        if (monoaotcompilerTimes.Count > 0)
            yield return new Counter { Name = "MonoAOTCompiler Time", MetricName = "ms", DefaultCounter = false, TopCounter = true, Results = monoaotcompilerTimes.ToArray() };
        if (appleappbuilderTimes.Count > 0)
            yield return new Counter { Name = "AppleAppBuilderTask Time", MetricName = "ms", DefaultCounter = false, TopCounter = true, Results = appleappbuilderTimes.ToArray() };
        if (publishTimes.Count > 0)
            yield return new Counter { Name = "Publish Time", MetricName = "ms", DefaultCounter = true, TopCounter = true, Results = publishTimes.ToArray() };
    }
}
