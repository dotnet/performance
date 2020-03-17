using System;
using System.Collections.Generic;
using System.Text;

namespace ScenarioMeasurement
{
    class LinuxTraceSession : ITraceSession
    {
        private Perfcollect perfcollect;
        private Dictionary<TraceSessionManager.KernelKeyword, Perfcollect.KernelKeyword> linKwMapKernel;
        private Dictionary<TraceSessionManager.ClrKeyword, Perfcollect.ClrKeyword> linKwMapClr;

        public LinuxTraceSession(string sessionName, string traceName, Logger logger)
        {
            perfcollect = new Perfcollect(traceName, logger);
            initLinuxKeywordMaps();
        }

        public void EnableProviders(IParser parser)
        {
            // Enable both providers and start the session
            parser.EnableKernelProvider(this);
            parser.EnableUserProviders(this);
            perfcollect.Start();
        }

        public void Dispose()
        {
            Console.WriteLine("Dispose not implemented.");
        }

        public void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords)
        {
            // Create keyword list for linux events
            var pcKernelKeywords = new List<Perfcollect.KernelKeyword>();
            foreach (var keyword in keywords)
            {
                pcKernelKeywords.Add(linKwMapKernel[keyword]);
            }
            perfcollect.KernelEvents = pcKernelKeywords;
        }

        public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
        {
            // Create keyword list for linux events
            var pcClrKeywords = new List<Perfcollect.ClrKeyword>();
            foreach (var keyword in keywords)
            {
                pcClrKeywords.Add(linKwMapClr[keyword]);
            }
            perfcollect.ClrEvents = pcClrKeywords;
        }

        private void initLinuxKeywordMaps()
        {
            // initialize linux kernel keyword map
            linKwMapKernel = new Dictionary<TraceSessionManager.KernelKeyword, Perfcollect.KernelKeyword>();
            linKwMapKernel[TraceSessionManager.KernelKeyword.Process] = Perfcollect.KernelKeyword.ProcessLifetime;
            linKwMapKernel[TraceSessionManager.KernelKeyword.Thread] = Perfcollect.KernelKeyword.Thread;
            linKwMapKernel[TraceSessionManager.KernelKeyword.ContextSwitch] = Perfcollect.KernelKeyword.ContextSwitch;

            // initialize linux clr keyword map
            linKwMapClr = new Dictionary<TraceSessionManager.ClrKeyword, Perfcollect.ClrKeyword>();
            linKwMapClr[TraceSessionManager.ClrKeyword.Startup] = Perfcollect.ClrKeyword.DotNETRuntimePrivate_StartupKeyword;
        }
    }
}
