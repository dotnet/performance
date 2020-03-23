using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace ScenarioMeasurement
{
    class WindowsTraceSession : ITraceSession
    {
        private Logger logger;
        private string traceName;
        private string traceDirectory;
        public TraceEventSession KernelSession { get; set; }
        public TraceEventSession UserSession { get; set; }
        private Dictionary<TraceSessionManager.KernelKeyword, KernelTraceEventParser.Keywords> winKwMapKernel;
        private Dictionary<TraceSessionManager.ClrKeyword, ClrPrivateTraceEventParser.Keywords> winKwMapClr;

        public WindowsTraceSession(string sessionName, string traceName, string traceDirectory, Logger logger)
        {
            this.traceName = traceName;
            this.traceDirectory = traceDirectory;
            this.logger = logger;

            string kernelFileName = Path.ChangeExtension(traceName, "perflabkernel.etl");
            string userFileName = Path.ChangeExtension(traceName, "perflabuser.etl");

            // Currently kernel and user share the same session
            KernelSession = new TraceEventSession(sessionName + "_kernel", Path.Combine(traceDirectory, kernelFileName));
            UserSession = new TraceEventSession(sessionName + "_user", Path.Combine(traceDirectory, userFileName));
            InitWindowsKeywordMaps();
        }

        public void EnableProviders(IParser parser)
        {
            // Enable both providers and start the session
            parser.EnableKernelProvider(this);
            parser.EnableUserProviders(this);
        }

        public void Dispose()
        {
            KernelSession.Dispose();
            UserSession.Dispose();

            string traceFilePath = Path.Combine(traceDirectory, Path.ChangeExtension(traceName, ".etl"));
            MergeFiles(KernelSession.FileName, UserSession.FileName, traceFilePath);
            logger.Log($"Trace Saved to {traceFilePath}");
        }

        private void MergeFiles(string kernelTraceFile, string userTraceFile, string traceFile)
        {
            var files = new List<string>();
            if (File.Exists(kernelTraceFile))
            {
                files.Add(kernelTraceFile);
            }
            if (File.Exists(userTraceFile))
            {
                files.Add(userTraceFile);
            }
            if (files.Count != 0)
            {
                logger.Log($"Merging {string.Join(',',files)}... ");
                TraceEventSession.Merge(files.ToArray(), traceFile);
                if (File.Exists(traceFile))
                {
                    File.Delete(userTraceFile);
                    File.Delete(kernelTraceFile);
                }
            }
        }

        public void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords)
        {
            // Create keyword flags for windows events
            KernelTraceEventParser.Keywords flags = 0;
            foreach (var keyword in keywords)
            {
                flags |= winKwMapKernel[keyword];
            }
            KernelSession.EnableKernelProvider(flags);
        }

        public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
        {
            // Create keyword flags for windows events
            ClrPrivateTraceEventParser.Keywords flags = 0;
            foreach (var keyword in keywords)
            {
                flags |= winKwMapClr[keyword];
            }
            UserSession.EnableProvider(ClrPrivateTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, (ulong)flags);
        }

        private void InitWindowsKeywordMaps()
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

        public void EnableUserProvider(string provider)
        {
            UserSession.EnableProvider(provider);
        }
    }
}
