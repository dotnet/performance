using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Data;
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
                if (!commandLine.Equals( ((string)GetPayloadValue(evt, "CommandLine")).Trim()))
                {
                    return false;
                }
            }
            // Match the command line as well because pids might be reused during the session
            return true;
        }

        public static bool MatchProcessName(TraceEvent evt, TraceSourceManager source, string processName)
        {
            if (source.IsWindows)
            {
                if (!processName.Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            else
            {
                // 15 characters is the maximum length of a process name in Linux kernel event payload
                if (processName.Length < 15)
                {
                    if (!processName.Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else 
                {
                    if (evt.PayloadByName("FileName") == null)
                    {
                        // match the first 15 characters only if FileName field is not present in the payload
                        if(!processName.Substring(0, 15).Equals(evt.ProcessName, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // match the full process name by extracting the file name
                        string filename = (string)GetPayloadValue(evt, "FileName");
                        if (!processName.Equals(Path.GetFileName(filename)))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static bool MatchProcessID(TraceEvent evt, TraceSourceManager source, IList<int> pids)
        {
            if (!pids.Contains(evt.ProcessID))
            {
                return false;
            }
            if (!source.IsWindows)
            {
                // For Linux both pid and tid should match
                if (!pids.Contains((int)GetPayloadValue(evt, "PayloadThreadID")))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool MatchSingleProcessID(TraceEvent evt, TraceSourceManager source, int pid)
        {
            return MatchProcessID(evt, source, new List<int> { pid});
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
    }
}
