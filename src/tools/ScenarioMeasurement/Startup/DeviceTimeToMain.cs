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
            throw new NotImplementedException();
        }

        public IEnumerable<Counter> Parse(string mergeTraceDirectory, string mergeTraceFilter, string processName, IList<int> pids, string commandLine)
        {
            var times = new List<double>();
            var files = new HashSet<string>();
            Console.WriteLine($"Finding files from {mergeTraceDirectory} with filter: {mergeTraceFilter}");
            foreach (var file in Directory.GetFiles(mergeTraceDirectory, mergeTraceFilter))
            {
                Console.WriteLine($"Found {file}");
                files.Add(file);
            }

            foreach (var trace in files)
            {
                Console.WriteLine($"Parsing {trace}");
                using (var source = new EventPipeEventSource(trace))
                {
                    source.Clr.MethodLoadVerbose += evt =>
                    {
                        if (evt.MethodName == "Main")
                        {
                            times.Add(evt.TimeStampRelativeMSec);
                        }
                    };
                    source.Process();
                }
            }

            return new[] { new Counter() { Name = "Generic Startup", MetricName = "ms", DefaultCounter = true, TopCounter = true, Results = times.ToArray() } };
        }
    }
}
