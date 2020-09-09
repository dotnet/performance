// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tracing;
using System.Collections.Generic;

namespace ScenarioMeasurement
{
    public class LinuxTraceSession : ITraceSession
    {
        public string TraceFilePath
        {
            get { return perfCollect?.TraceFilePath; }
        }
        private readonly PerfCollect perfCollect;
        private Dictionary<TraceSessionManager.KernelKeyword, PerfCollect.KernelKeyword> kernelKeywords;
        private Dictionary<TraceSessionManager.ClrKeyword, PerfCollect.ClrKeyword> clrKeywords;

        public LinuxTraceSession(string sessionName, string traceName, string traceDirectory, Logger logger)
        {
            perfCollect = new PerfCollect(traceName, traceDirectory, logger);
            InitLinuxKeywordMaps();
        }

        public void EnableProviders(IParser parser)
        {
            // Enable both providers and start the session
            parser.EnableKernelProvider(this);
            parser.EnableUserProviders(this);
            perfCollect.Start();
        }

        public void Dispose()
        {
            perfCollect.Stop();
        }

        public void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords)
        {
            foreach (var keyword in keywords)
            {
                perfCollect.AddKernelKeyword(kernelKeywords[keyword]);
            }
        }

        public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
        {
            foreach (var keyword in keywords)
            {
                perfCollect.AddClrKeyword(clrKeywords[keyword]);
            }
        }

        private void InitLinuxKeywordMaps()
        {
            // initialize linux kernel keyword map
            kernelKeywords = new Dictionary<TraceSessionManager.KernelKeyword, PerfCollect.KernelKeyword>
            {
                [TraceSessionManager.KernelKeyword.Process] = PerfCollect.KernelKeyword.LTTng_Kernel_ProcessLifetimeKeyword,
                [TraceSessionManager.KernelKeyword.Thread] = PerfCollect.KernelKeyword.LTTng_Kernel_ThreadKeyword,
                [TraceSessionManager.KernelKeyword.ContextSwitch] = PerfCollect.KernelKeyword.LTTng_Kernel_ContextSwitchKeyword
            };

            // initialize linux clr keyword map
            clrKeywords = new Dictionary<TraceSessionManager.ClrKeyword, PerfCollect.ClrKeyword>
            {
                [TraceSessionManager.ClrKeyword.Startup] = PerfCollect.ClrKeyword.DotNETRuntimePrivate_StartupKeyword
            };
        }

        public void EnableUserProvider(string provider, TraceEventLevel verboseLevel = TraceEventLevel.Verbose)
        {
            // Enable all EventSource events on Linux
            perfCollect.AddClrKeyword(PerfCollect.ClrKeyword.EventSource);
            // Filter events from the provider
            Startup.AddTestProcessEnvironmentVariable("COMPlus_EventSourceFilter", provider);
        }
    }
}
