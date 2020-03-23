using System;
using System.Collections.Generic;
using System.Text;

namespace ScenarioMeasurement
{
    class LinuxTraceSession : ITraceSession
    {
        private PerfCollect perfCollect;
        private Dictionary<TraceSessionManager.KernelKeyword, PerfCollect.KernelKeyword> linKwMapKernel;
        private Dictionary<TraceSessionManager.ClrKeyword, PerfCollect.ClrKeyword> linKwMapClr;

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
                perfCollect.AddKernelKeyword(linKwMapKernel[keyword]);
            }
        }

        public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
        {
            foreach (var keyword in keywords)
            {
                perfCollect.AddClrKeyword(linKwMapClr[keyword]);
            }
        }

        private void InitLinuxKeywordMaps()
        {
            // initialize linux kernel keyword map
            linKwMapKernel = new Dictionary<TraceSessionManager.KernelKeyword, PerfCollect.KernelKeyword>();
            linKwMapKernel[TraceSessionManager.KernelKeyword.Process] = PerfCollect.KernelKeyword.ProcessLifetime;
            linKwMapKernel[TraceSessionManager.KernelKeyword.Thread] = PerfCollect.KernelKeyword.Thread;
            linKwMapKernel[TraceSessionManager.KernelKeyword.ContextSwitch] = PerfCollect.KernelKeyword.ContextSwitch;

            // initialize linux clr keyword map
            linKwMapClr = new Dictionary<TraceSessionManager.ClrKeyword, PerfCollect.ClrKeyword>();
            linKwMapClr[TraceSessionManager.ClrKeyword.Startup] = PerfCollect.ClrKeyword.DotNETRuntimePrivate_StartupKeyword;
        }

        public void EnableUserProvider(string provider)
        {
        }
    }
}
