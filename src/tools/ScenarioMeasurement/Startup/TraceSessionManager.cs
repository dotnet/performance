﻿using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;

namespace ScenarioMeasurement
{

    public interface ITraceSession : IDisposable
    {
        string TraceFilePath { get; }
        void EnableProviders(IParser parser);
        void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords);
        void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords);
        void EnableUserProvider(string provider);
    }

    public static class TraceSessionManager
    {
        public static bool IsWindows { get { return Util.IsWindows(); } }
        public static ITraceSession CreateSession(string sessionName, string traceName, string traceDirectory, Logger logger)
        {

            if (IsWindows)
            {
                return new WindowsTraceSession(sessionName, traceName, traceDirectory, logger);
            }
            else
            {
                return new LinuxTraceSession(sessionName, traceName, traceDirectory, logger);
            }
        }

        public enum KernelKeyword
        {
            Process,
            Thread,
            ContextSwitch
        }

        public enum ClrKeyword
        {
            Startup
        }
    }


}

