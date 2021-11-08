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
    /// 
    /// Convention: If a trace is named with a 1, such as trace1.nettrace, if exist trace2.nettrace..traceN.nettrace, 
    /// those will be parsed as well and added to the resultant collection.
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
            var files = new HashSet<string>();
            files.Add(mergeTraceFile);
            var traceName = Path.GetFileNameWithoutExtension(mergeTraceFile);
            if (traceName.EndsWith("1"))
            {
                traceName = traceName.TrimEnd('1');
                var dirName = Path.GetDirectoryName(mergeTraceFile);
                foreach (var file in Directory.GetFiles(dirName, $"{traceName}?.nettrace"))
                {
                    Console.WriteLine($"Found {file}");
                    files.Add(file);
                }
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
