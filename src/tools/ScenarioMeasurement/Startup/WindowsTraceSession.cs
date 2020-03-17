using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;
using System.IO;


namespace ScenarioMeasurement
{
    class WindowsTraceSession : ITraceSession
    {
        private TraceEventSession session;
        private Dictionary<TraceSessionManager.KernelKeyword, KernelTraceEventParser.Keywords> winKwMapKernel;
        private Dictionary<TraceSessionManager.ClrKeyword, ClrPrivateTraceEventParser.Keywords> winKwMapClr;

        public WindowsTraceSession(string sessionName, string traceName, Logger logger)
        {
            string fileName = Path.ChangeExtension(traceName, "perflab.etl");
            // Currently kernel and user share the same session
            session = new TraceEventSession(sessionName, fileName);
            initWindowsKeywordMaps();
        }

        public void EnableProviders(IParser parser)
        {
            // Enable both providers and start the session
            parser.EnableKernelProvider(this);
            parser.EnableUserProviders(this);
        }

        public void Dispose()
        {
            session.Dispose();
        }

        public void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords)
        {
            // Create keyword flags for windows events
            KernelTraceEventParser.Keywords flags = 0;
            foreach (var keyword in keywords)
            {
                flags |= winKwMapKernel[keyword];
            }
            session.EnableKernelProvider(flags);
        }

        public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
        {
            // Create keyword flags for windows events
            ClrPrivateTraceEventParser.Keywords flags = 0;
            foreach (var keyword in keywords)
            {
                flags |= winKwMapClr[keyword];
            }
            session.EnableProvider(ClrPrivateTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, (ulong)flags);
        }

        private void initWindowsKeywordMaps()
        {
            // initialize windows kernel keyword map
            winKwMapKernel = new Dictionary<TraceSessionManager.KernelKeyword, KernelTraceEventParser.Keywords>();
            winKwMapKernel[TraceSessionManager.KernelKeyword.Process] = KernelTraceEventParser.Keywords.Process;
            winKwMapKernel[TraceSessionManager.KernelKeyword.Thread] = KernelTraceEventParser.Keywords.Thread;
            winKwMapKernel[TraceSessionManager.KernelKeyword.ContextSwitch] = KernelTraceEventParser.Keywords.ContextSwitch;

            // initialize windows clr keyword map
            winKwMapClr = new Dictionary<TraceSessionManager.ClrKeyword, ClrPrivateTraceEventParser.Keywords>();
            winKwMapClr[TraceSessionManager.ClrKeyword.Startup] = ClrPrivateTraceEventParser.Keywords.Startup;
        }
    }
}
