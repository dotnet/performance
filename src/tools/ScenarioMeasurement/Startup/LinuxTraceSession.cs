using System.Collections.Generic;

namespace ScenarioMeasurement
{
    public class LinuxTraceSession : ITraceSession
    {
        public string TraceFilePath { get { return perfCollect?.TraceFilePath; } }
        private PerfCollect perfCollect;
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
            kernelKeywords = new Dictionary<TraceSessionManager.KernelKeyword, PerfCollect.KernelKeyword>();
            kernelKeywords[TraceSessionManager.KernelKeyword.Process] = PerfCollect.KernelKeyword.ProcessLifetime;
            kernelKeywords[TraceSessionManager.KernelKeyword.Thread] = PerfCollect.KernelKeyword.Thread;
            kernelKeywords[TraceSessionManager.KernelKeyword.ContextSwitch] = PerfCollect.KernelKeyword.ContextSwitch;

            // initialize linux clr keyword map
            clrKeywords = new Dictionary<TraceSessionManager.ClrKeyword, PerfCollect.ClrKeyword>();
            clrKeywords[TraceSessionManager.ClrKeyword.Startup] = PerfCollect.ClrKeyword.DotNETRuntimePrivate_StartupKeyword;
        }

        public void EnableUserProvider(string provider)
        {
        }
    }
}
