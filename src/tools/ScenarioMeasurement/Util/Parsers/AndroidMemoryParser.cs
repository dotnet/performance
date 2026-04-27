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
    /// This is a custom parser that does not enable any profiling. Instead, it relies on being passed files collected
    /// off the test machine (usually on a device.) 
    /// </summary>
    public class AndroidMemoryParser : IParser
    {
        public void EnableKernelProvider(ITraceSession kernel)
        {
            throw new NotSupportedException();
        }

        public void EnableUserProviders(ITraceSession user)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
        {
            var pssMins = new List<double>();
            var pssAvgs = new List<double>();
            var pssMaxs = new List<double>();
            var ussMins = new List<double>();
            var ussAvgs = new List<double>();
            var ussMaxs = new List<double>();
            var rssMins = new List<double>();
            var rssAvgs = new List<double>();
            var rssMaxs = new List<double>();
            var captureCounts = new List<double>();
            var counters = new List<Counter>();

            if (File.Exists(mergeTraceFile))
            {
                using (StreamReader sr = new StreamReader(mergeTraceFile))
                {
                    string line = sr.ReadToEnd();
                    MatchCollection finds = Regex.Matches(line, @"PSS: min (?<pssMin>\d+), avg (?<pssAvg>\d+), max (?<pssMax>\d+); USS: min (?<ussMin>\d+), avg (?<ussAvg>\d+), max (?<ussMax>\d+); RSS: min (?<rssMin>\d+), avg (?<rssAvg>\d+), max (?<rssMax>\d+); Number: (?<captureNumber>\d+)");
                    Console.WriteLine($"Found # of MemoryConsumption Tests: {finds.Count}");
                    foreach (Match match in finds)
                    {
                        GroupCollection groups = match.Groups;
                        Console.WriteLine($"Found MemoryConsumption (MB) - PSS: min {groups["pssMin"].Value}, avg {groups["pssAvg"].Value}, max {groups["pssMax"].Value}; USS: min {groups["ussMin"].Value}, avg {groups["ussAvg"].Value}, max {groups["ussMax"].Value}; RSS: min {groups["rssMin"].Value}, avg {groups["rssAvg"].Value}, max {groups["rssMax"].Value}; Number: {groups["captureNumber"].Value}");
                        pssMins.Add(Double.Parse(groups["pssMin"].Value));
                        pssAvgs.Add(Double.Parse(groups["pssAvg"].Value));
                        pssMaxs.Add(Double.Parse(groups["pssMax"].Value));
                        ussMins.Add(Double.Parse(groups["ussMin"].Value));
                        ussAvgs.Add(Double.Parse(groups["ussAvg"].Value));
                        ussMaxs.Add(Double.Parse(groups["ussMax"].Value));
                        rssMins.Add(Double.Parse(groups["rssMin"].Value));
                        rssAvgs.Add(Double.Parse(groups["rssAvg"].Value));
                        rssMaxs.Add(Double.Parse(groups["rssMax"].Value));
                        captureCounts.Add(Double.Parse(groups["captureNumber"].Value));
                    }
                }
            }

            counters.Add(new Counter() { Name = "PSS Min", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = pssMins.ToArray() });
            counters.Add(new Counter() { Name = "PSS Avg", MetricName = "MB", DefaultCounter = true, TopCounter = true, Results = pssAvgs.ToArray() });
            counters.Add(new Counter() { Name = "PSS Max", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = pssMaxs.ToArray() });
            counters.Add(new Counter() { Name = "USS Min", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = ussMins.ToArray() });
            counters.Add(new Counter() { Name = "USS Avg", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = ussAvgs.ToArray() });
            counters.Add(new Counter() { Name = "USS Max", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = ussMaxs.ToArray() });
            counters.Add(new Counter() { Name = "RSS Min", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = rssMins.ToArray() });
            counters.Add(new Counter() { Name = "RSS Avg", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = rssAvgs.ToArray() });
            counters.Add(new Counter() { Name = "RSS Max", MetricName = "MB", DefaultCounter = false, TopCounter = true, Results = rssMaxs.ToArray() });
            counters.Add(new Counter() { Name = "Capture Count", MetricName = "Count", DefaultCounter = false, TopCounter = true, Results = captureCounts.ToArray() });
            return counters;
        }
    }
}
