using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ScenarioMeasurement;


/// <summary>
/// This is a custom parser that does not enable any profiling. Instead, it relies on being passed nettrace files collected
/// off the test machine (usually on a device.) 
/// </summary>
public class DeviceTimeToMain : IParser
{
    public void EnableKernelProvider(ITraceSession kernel)
    {
        throw new NotImplementedException();
    }

    public void EnableUserProviders(ITraceSession user)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
    {
        var times = new List<double>();
        var totalTimePattern = new Regex(@"TotalTime:\s(?<totalTime>.+)");

        if (File.Exists(mergeTraceFile))
        {
            using(var sr = new StreamReader(mergeTraceFile))
            {
                var line = sr.ReadToEnd();
                var finds = totalTimePattern.Matches(line);
                Console.WriteLine($"Found Startup Times: {finds.Count}");
                foreach (Match match in finds)
                {
                    var groups = match.Groups;
                    Console.WriteLine($"Found Value (ms): {groups["totalTime"].Value}");
                    times.Add(double.Parse(groups["totalTime"].Value));
                }
            }
        }

        return new[] { new Counter() { Name = "Generic Startup", MetricName = "ms", DefaultCounter = true, TopCounter = true, Results = times.ToArray() } };
    }
}
