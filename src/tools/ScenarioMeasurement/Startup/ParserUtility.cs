using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ScenarioMeasurement
{
    class ParserUtility
    {
        public static bool MatchProcess(TraceEvent evt, TraceSourceManager source, string processName, IList<int> pids, string commandLine)
        {
            if (!pids.Contains(evt.ProcessID))
            {
                return false;
            }
            if (source.IsWindows)
            {
                if (!processName.Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                // Match the command line as well because pids might be reused during the session
                if (((string)GetPayloadValue(evt, "CommandLine")).Trim() != commandLine)
                {
                    return false;
                }
            }
            else
            {
                // For Linux both pid and tid should match
                if (!pids.Contains((int)GetPayloadValue(evt,"PayloadThreadID"))){
                    return false;
                }
                // 15 characters is the maximum length of a process name in Linux kernel event payload
                if (processName.Length < 15)
                {
                    if (!processName.Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else if (!processName.Substring(0, 15).Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool MatchProcessByPid(TraceEvent evt, TraceSourceManager source, int pid, string processName, string commandLine)
        {
            return MatchProcess(evt, source, processName, new List<int> { pid }, commandLine);
        }

        public static object GetPayloadValue(TraceEvent evt, string payloadName)
        {
            var result = evt.PayloadByName(payloadName);
            if (result == null)
            {
                throw new NoNullAllowedException($"Payload \"{payloadName}\" doesn't exist in event \"{evt.EventName}\" ");
            }
            return result;
        }
    }
}
