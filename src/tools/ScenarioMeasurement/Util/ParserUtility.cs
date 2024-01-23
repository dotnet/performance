using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;


namespace ScenarioMeasurement
{
    class ParserUtility
    {
        public static bool MatchProcessStart(TraceEvent evt, TraceSourceManager source, string processName, IList<int> pids, string commandLine)
        {
            return MatchCommandLine(evt, source, commandLine) &&
                   MatchProcessName(evt, source, processName) &&
                   MatchProcessID(evt, source, pids);
        }

        public static bool MatchCommandLine(TraceEvent evt, TraceSourceManager source, string commandLine)
        {
            if (source.IsWindows)
            {
                int bufferMax = 512;
                string payloadCommandLine = (string)GetPayloadValue(evt, "CommandLine");
                if (payloadCommandLine.Length >= bufferMax && commandLine.Length >= bufferMax)
                {
                    commandLine = commandLine.Substring(0, bufferMax);
                    payloadCommandLine = payloadCommandLine.Substring(0, bufferMax);
                }
                if (!commandLine.Trim().Equals(payloadCommandLine.Trim()))
                {
                    return CompareResult.Mismatch;
                }
            }
            else
            {
                return CompareResult.NotApplicable;
            }
            // Match the command line as well because pids might be reused during the session
            return CompareResult.Match;
        }

        public static bool MatchProcessName(TraceEvent evt, TraceSourceManager source, string processName)
        {
            if (source.IsWindows)
            {
                if (!processName.Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return CompareResult.Mismatch;
                }
            }
            else
            {
                // 15 characters is the maximum length of a process name in Linux kernel event payload
                if (processName.Length < 15)
                {
                    if (!processName.Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        return CompareResult.Mismatch;
                    }
                }
                else
                {
                    if (evt.PayloadByName("FileName") == null)
                    {
                        // match the first 15 characters only if FileName field is not present in the payload
                        if (!processName.Substring(0, 15).Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                        {
                            return CompareResult.Mismatch;
                        }
                    }
                    else
                    {
                        // match the full process name by extracting the file name
                        string filename = (string)GetPayloadValue(evt, "FileName");
                        if (!processName.Equals(Path.GetFileName(filename)))
                        {
                            return CompareResult.Mismatch;
                        }
                    }
                }
            }
            return CompareResult.Match;
        }

        public static bool MatchProcessID(TraceEvent evt, TraceSourceManager source, IList<int> pids)
        {
            if (!pids.Contains(evt.ProcessID))
            {
                return CompareResult.Mismatch;
            }
            if (!source.IsWindows)
            {
                // For Linux both pid and tid should match
                // Check default payload value
                if (!pids.Contains(evt.ThreadID))
                {
                    // Check event-specific payload value
                    if (!pids.Contains((int)GetPayloadValue(evt, "PayloadThreadID")))
                    {
                        return CompareResult.Mismatch;
                    }
                }
            }
            return CompareResult.Match;
        }

        public static bool MatchSingleProcessID(TraceEvent evt, TraceSourceManager source, int pid)
        {
            return MatchProcessID(evt, source, new List<int> { pid });
        }

        private static object GetPayloadValue(TraceEvent evt, string payloadName)
        {
            var result = evt.PayloadByName(payloadName);
            if (result == null)
            {
                throw new NoNullAllowedException($"Payload \"{payloadName}\" doesn't exist in event \"{evt.EventName}\" ");
            }
            return result;
        }

        public sealed class CompareResult
        {
            public static readonly bool Match = true;
            public static readonly bool Mismatch = false;
            public static readonly bool NotApplicable = CompareResult.Match;
        }
    }
}
