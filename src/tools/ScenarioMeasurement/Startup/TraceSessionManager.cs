// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tracing;
using System;

namespace ScenarioMeasurement
{
    public interface ITraceSession : IDisposable
    {
        string TraceFilePath { get; }
        void EnableProviders(IParser parser);
        void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords);
        void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords);
        void EnableUserProvider(string provider, TraceEventLevel verboseLevel);
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

