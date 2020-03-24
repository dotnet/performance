using System;

namespace ScenarioMeasurement
{

    public interface ITraceSession : IDisposable
    {
        void EnableProviders(IParser parser);
        void EnableKernelProvider(params TraceSessionManager.KernelKeyword[] keywords);
        void EnableUserProvider(params TraceSessionManager.ClrKeyword[] keywords);
        void EnableUserProvider(string provider);
        string GetTraceFilePath();
    }

    public static class TraceSessionManager
    {
        public static ITraceSession CreateSession(string sessionName, string traceName, string traceDirectory, Logger logger)
        {

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return new WindowsTraceSession(sessionName, traceName, traceDirectory, logger);
            }
            else
            {
                return new LinuxTraceSession(sessionName, traceName, traceDirectory, logger);
            }
        }

        public static ITraceSession CreateProfileSession(string sessionName, string traceName, string traceDirectory, Logger logger)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return new WindowsTraceSession(sessionName, traceName, traceDirectory, logger);
            }
            throw new NotImplementedException("Profile iteration not implemented on Linux");
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
