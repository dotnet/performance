using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ScenarioMeasurement
{

    /// <summary>
    /// This is a custom parser that does not enable any profiling. Instead, it relies on being passed nettrace files collected
    /// off the test machine (usually on a device.) 
    /// </summary>
    class DeviceTimeToMain : IParser
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
            Console.WriteLine($"In the parser!!! File: {mergeTraceFile}");
            Regex totalTimePattern = new Regex(@"TotalTime:\s(?<totalTime>.+)");

            if (File.Exists(mergeTraceFile))
            {
                using(StreamReader sr = new StreamReader(mergeTraceFile))
                {
                    string line = sr.ReadToEnd();
                    MatchCollection finds = totalTimePattern.Matches(line);
                    Console.WriteLine($"Finds: {finds.Count}");
                    foreach (Match match in finds)
                    {
                        GroupCollection groups = match.Groups;
                        Console.WriteLine(groups["totalTime"].Value);
                        times.Add(Double.Parse(groups["totalTime"].Value));
                    }
                }
            }

            return new[] { new Counter() { Name = "Generic Startup", MetricName = "ms", DefaultCounter = true, TopCounter = true, Results = times.ToArray() } };
        }
    }
}
