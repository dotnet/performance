using System;
using System.IO;

namespace ScenarioMeasurement
{

    public interface ITraceSession : IDisposable
    {
        void EnableProviders(IParser parser);
        void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords);
        void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords);
    }

    public static class TraceSessionManager
    {
        public static ITraceSession CreateSession(string sessionName, string traceName, Logger logger)
        {
            /*
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            return new WindowsTraceSession(sessionName, traceName, logger);
                        }
                        else
                        {
                            return new LinuxTraceSession(sessionName, traceName, logger);
                        }*/
            return new LinuxTraceSession(sessionName, traceName, logger);
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
