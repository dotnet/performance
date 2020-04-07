using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;
using System.IO;


namespace ScenarioMeasurement
{
    public class WindowsTraceSession : ITraceSession
    {
        private Logger logger;
        public string TraceFilePath { get; }
        public TraceEventSession KernelSession { get; set; }
        public TraceEventSession UserSession { get; set; }
        private Dictionary<TraceSessionManager.KernelKeyword, KernelTraceEventParser.Keywords> kernelKeywords;
        private Dictionary<TraceSessionManager.ClrKeyword, ClrPrivateTraceEventParser.Keywords> clrKeywords;

        public WindowsTraceSession(string sessionName, string traceName, string traceDirectory, Logger logger)
        {
            this.logger = logger;

            string kernelFileName = Path.ChangeExtension(traceName, "perflabkernel.etl");
            string userFileName = Path.ChangeExtension(traceName, "perflabuser.etl");
            TraceFilePath = Path.Combine(traceDirectory, Path.ChangeExtension(traceName, ".etl"));

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

            MergeFiles(KernelSession.FileName, UserSession.FileName, TraceFilePath);
            logger.Log($"Trace Saved to {TraceFilePath}");
        }

        private void MergeFiles(string kernelTraceFile, string userTraceFile, string traceFile)
        {
            var files = new List<string>();
            if (!File.Exists(kernelTraceFile))
            {
                throw new FileNotFoundException("Kernel trace file not found.");
            }
            files.Add(kernelTraceFile);
            if (File.Exists(userTraceFile))
            {
                files.Add(userTraceFile);
            }
            
            logger.Log($"Merging {string.Join(',',files)}... ");
            TraceEventSession.Merge(files.ToArray(), traceFile);
            if (File.Exists(traceFile))
            {
                File.Delete(userTraceFile);
                File.Delete(kernelTraceFile);
            }
        }

        public void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords)
        {
            // Create keyword flags for windows events
            KernelTraceEventParser.Keywords flags = 0;
            foreach (var keyword in keywords)
            {
                flags |= kernelKeywords[keyword];
            }
            KernelSession.EnableKernelProvider(flags);
        }

        public void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords)
        {
            // Create keyword flags for windows events
            ClrPrivateTraceEventParser.Keywords flags = 0;
            foreach (var keyword in keywords)
            {
                flags |= clrKeywords[keyword];
            }
            UserSession.EnableProvider(ClrPrivateTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, (ulong)flags);
        }

        private void InitWindowsKeywordMaps()
        {
            // initialize windows kernel keyword map
            kernelKeywords = new Dictionary<TraceSessionManager.KernelKeyword, KernelTraceEventParser.Keywords>();
            kernelKeywords[TraceSessionManager.KernelKeyword.Process] = KernelTraceEventParser.Keywords.Process;
            kernelKeywords[TraceSessionManager.KernelKeyword.Thread] = KernelTraceEventParser.Keywords.Thread;
            kernelKeywords[TraceSessionManager.KernelKeyword.ContextSwitch] = KernelTraceEventParser.Keywords.ContextSwitch;

            // initialize windows clr keyword map
            clrKeywords = new Dictionary<TraceSessionManager.ClrKeyword, ClrPrivateTraceEventParser.Keywords>();
            clrKeywords[TraceSessionManager.ClrKeyword.Startup] = ClrPrivateTraceEventParser.Keywords.Startup;
        }

        public void EnableUserProvider(string provider)
        {
            UserSession.EnableProvider(provider);
        }
    }
}
