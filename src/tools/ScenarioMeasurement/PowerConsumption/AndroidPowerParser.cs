using Microsoft.Diagnostics.Tracing;
using Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ScenarioMeasurement
{
    /// <summary>
    /// This is a custom parser that does not enable any profiling. Instead, it relies on being passed files collected
    /// off the test machine (usually on a device.)
    /// </summary>
    class AndroidPowerParser : IParser
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
            // Single data line: "totalPowermAh": "0.0886", "isSystemBatteryConsumer": "0", "screenPowermAh": "0.133", "proportionalPowermAh": "0.0250", "foregroundTimeMs": "10311", "foregroundTimeCount": "0", "cpuTimeUserMs": "668", "cpuTimeSystemMs": "147"
            var totalPowermAh = new List<double>();
            var isSystemBatteryConsumer = new List<double>();
            var screenPowermAh = new List<double>();
            var proportionalPowermAh = new List<double>();
            var foregroundTimeMs = new List<double>();
            var foregroundTimeCount = new List<double>();
            var cpuTimeUserMs = new List<double>();
            var cpuTimeSystemMs = new List<double>();
            var counters = new List<Counter>();

            if (File.Exists(mergeTraceFile))
            {
                var jsonText = File.ReadAllText(mergeTraceFile);
                using (JsonDocument document = JsonDocument.Parse(jsonText)){
                    var root = document.RootElement;
                    foreach (var test in root.EnumerateArray())
                    {
                        totalPowermAh.Add(Double.Parse(test.GetProperty("totalPowermAh").GetString()));
                        isSystemBatteryConsumer.Add(Double.Parse(test.GetProperty("isSystemBatteryConsumer").GetString()));
                        screenPowermAh.Add(Double.Parse(test.GetProperty("screenPowermAh").GetString()));
                        proportionalPowermAh.Add(Double.Parse(test.GetProperty("proportionalPowermAh").GetString()));
                        foregroundTimeMs.Add(Double.Parse(test.GetProperty("foregroundTimeMs").GetString()));
                        foregroundTimeCount.Add(Double.Parse(test.GetProperty("foregroundTimeCount").GetString()));
                        cpuTimeUserMs.Add(Double.Parse(test.GetProperty("cpuTimeUserMs").GetString()));
                        cpuTimeSystemMs.Add(Double.Parse(test.GetProperty("cpuTimeSystemMs").GetString()));
                    }
                }

            }

            counters.Add(new Counter() { Name = "Total Power Usage", MetricName = "mAh", DefaultCounter = true, TopCounter = true, Results = totalPowermAh.ToArray() });
            counters.Add(new Counter() { Name = "Is System Battery Consumer", MetricName = "Bool", DefaultCounter = false, TopCounter = false, Results = isSystemBatteryConsumer.ToArray() });
            counters.Add(new Counter() { Name = "Screen Power Usage", MetricName = "mAh", DefaultCounter = false, TopCounter = true, Results = screenPowermAh.ToArray() });
            counters.Add(new Counter() { Name = "Proportional Power Usage", MetricName = "mAh", DefaultCounter = false, TopCounter = true, Results = proportionalPowermAh.ToArray() });
            counters.Add(new Counter() { Name = "Foreground Time", MetricName = "ms", DefaultCounter = false, TopCounter = true, Results = foregroundTimeMs.ToArray() });
            counters.Add(new Counter() { Name = "Foreground Time Count", MetricName = "Count", DefaultCounter = false, TopCounter = false, Results = foregroundTimeCount.ToArray() });
            counters.Add(new Counter() { Name = "CPU User Time", MetricName = "ms", DefaultCounter = false, TopCounter = true, Results = cpuTimeUserMs.ToArray() });
            counters.Add(new Counter() { Name = "CPU System Time", MetricName = "ms", DefaultCounter = false, TopCounter = true, Results = cpuTimeSystemMs.ToArray() });
            return counters;
        }
    }
}
