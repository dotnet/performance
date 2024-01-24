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
    public class AndroidPowerParser : IParser
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
            var cpuTimeTotalMs = new List<double>();
            var preExecutionBatteryLevel = new List<double>();
            var preExecutionBatteryVoltage = new List<double>();
            var postExecutionBatteryLevel = new List<double>();
            var postExecutionBatteryVoltage = new List<double>();
            var executionBatteryLevelChange = new List<double>();
            var executionBatteryVoltageChange = new List<double>();
            var totalJouleUsageEstimate = new List<double>();
            var screenJouleUsageEstimate = new List<double>();
            var proportionalJouleUsageEstimate = new List<double>();
            var allJouleUsageEstimate = new List<double>();
            var counters = new List<Counter>();

            if (File.Exists(mergeTraceFile))
            {
                var jsonText = File.ReadAllText(mergeTraceFile);
                using (JsonDocument document = JsonDocument.Parse(jsonText))
                {
                    var root = document.RootElement;
                    foreach (var test in root.EnumerateArray())
                    {
                        var cpuTimeUserMsValue = Double.Parse(test.GetProperty("cpuTimeUserMs").GetString());
                        var cpuTimeSystemMsValue = Double.Parse(test.GetProperty("cpuTimeSystemMs").GetString());
                        var totalCpuTimeMsValue = cpuTimeUserMsValue + cpuTimeSystemMsValue;
                        var totalPowermAhValue = Double.Parse(test.GetProperty("totalPowermAh").GetString());
                        var screenPowermAhValue = Double.Parse(test.GetProperty("screenPowermAh").GetString());
                        var proportionalPowermAhValue = Double.Parse(test.GetProperty("proportionalPowermAh").GetString());
                        var preExecutionBatteryLevelValue = Double.Parse(test.GetProperty("preExecutionBatteryLevel").GetString());
                        var preExecutionBatteryVoltageValue = Double.Parse(test.GetProperty("preExecutionBatteryVoltage").GetString());
                        var postExecutionBatteryLevelValue = Double.Parse(test.GetProperty("postExecutionBatteryLevel").GetString());
                        var postExecutionBatteryVoltageValue = Double.Parse(test.GetProperty("postExecutionBatteryVoltage").GetString());
                        var executionBatteryLevelChangeValue = preExecutionBatteryLevelValue - postExecutionBatteryLevelValue;
                        var executionBatteryVoltageChangeValue = preExecutionBatteryVoltageValue - postExecutionBatteryVoltageValue;
                        // Joule calculation: mAh X voltage x 3.6 = Joules (https://www.axconnectorlubricant.com/rce/battery-electronics-101.html#:~:text=Choosing%201%20Volts%20for%20the%20voltage%2C%20we%20can,X%20voltage%20x%203.6%20%3D%20Joules%20of%20energy.)
                        var estimatedBatteryVoltageValueForJouleCalculation = (preExecutionBatteryVoltageValue + postExecutionBatteryVoltageValue) / (2 * 1000); // change from mV to V and get the average (we assume a small change in voltage during the test)
                        var totalJouleUsageEstimateValue = totalPowermAhValue * 3.6 * estimatedBatteryVoltageValueForJouleCalculation;
                        var screenJouleUsageEstimateValue = screenPowermAhValue * 3.6 * estimatedBatteryVoltageValueForJouleCalculation;
                        var proportionalJouleUsageEstimateValue = proportionalPowermAhValue * 3.6 * estimatedBatteryVoltageValueForJouleCalculation;
                        var allJouleUsageEstimateValue = totalJouleUsageEstimateValue + screenJouleUsageEstimateValue + proportionalJouleUsageEstimateValue;

                        totalPowermAh.Add(totalPowermAhValue);
                        isSystemBatteryConsumer.Add(Double.Parse(test.GetProperty("isSystemBatteryConsumer").GetString()));
                        screenPowermAh.Add(screenPowermAhValue);
                        proportionalPowermAh.Add(proportionalPowermAhValue);
                        foregroundTimeMs.Add(Double.Parse(test.GetProperty("foregroundTimeMs").GetString()));
                        foregroundTimeCount.Add(Double.Parse(test.GetProperty("foregroundTimeCount").GetString()));
                        cpuTimeUserMs.Add(cpuTimeUserMsValue);
                        cpuTimeSystemMs.Add(cpuTimeSystemMsValue);
                        cpuTimeTotalMs.Add(totalCpuTimeMsValue);
                        preExecutionBatteryLevel.Add(preExecutionBatteryLevelValue);
                        preExecutionBatteryVoltage.Add(preExecutionBatteryVoltageValue);
                        postExecutionBatteryLevel.Add(postExecutionBatteryLevelValue);
                        postExecutionBatteryVoltage.Add(postExecutionBatteryVoltageValue);
                        executionBatteryLevelChange.Add(executionBatteryLevelChangeValue);
                        executionBatteryVoltageChange.Add(executionBatteryVoltageChangeValue);
                        totalJouleUsageEstimate.Add(totalJouleUsageEstimateValue);
                        screenJouleUsageEstimate.Add(screenJouleUsageEstimateValue);
                        proportionalJouleUsageEstimate.Add(proportionalJouleUsageEstimateValue);
                        allJouleUsageEstimate.Add(allJouleUsageEstimateValue);
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
            counters.Add(new Counter() { Name = "Pre Execution Battery Level", MetricName = "Percent", DefaultCounter = false, TopCounter = false, Results = preExecutionBatteryLevel.ToArray() });
            counters.Add(new Counter() { Name = "Pre Execution Battery Voltage", MetricName = "MilliVolt", DefaultCounter = false, TopCounter = false, Results = preExecutionBatteryVoltage.ToArray() });
            counters.Add(new Counter() { Name = "Post Execution Battery Level", MetricName = "Percent", DefaultCounter = false, TopCounter = false, Results = postExecutionBatteryLevel.ToArray() });
            counters.Add(new Counter() { Name = "Post Execution Battery Voltage", MetricName = "MilliVolt", DefaultCounter = false, TopCounter = false, Results = postExecutionBatteryVoltage.ToArray() });
            counters.Add(new Counter() { Name = "Execution Battery Level Change", MetricName = "Percent", DefaultCounter = false, TopCounter = false, Results = executionBatteryLevelChange.ToArray() });
            counters.Add(new Counter() { Name = "Execution Battery Voltage Change", MetricName = "MilliVolt", DefaultCounter = false, TopCounter = false, Results = executionBatteryVoltageChange.ToArray() });
            counters.Add(new Counter() { Name = "Total Joule Usage Estimate", MetricName = "Joule", DefaultCounter = false, TopCounter = true, Results = totalJouleUsageEstimate.ToArray() });
            counters.Add(new Counter() { Name = "Screen Joule Usage Estimate", MetricName = "Joule", DefaultCounter = false, TopCounter = true, Results = screenJouleUsageEstimate.ToArray() });
            counters.Add(new Counter() { Name = "Proportional Joule Usage Estimate", MetricName = "Joule", DefaultCounter = false, TopCounter = true, Results = proportionalJouleUsageEstimate.ToArray() });
            counters.Add(new Counter() { Name = "All Joule Usage Estimate", MetricName = "Joule", DefaultCounter = false, TopCounter = true, Results = allJouleUsageEstimate.ToArray() });
            return counters;
        }
    }
}
