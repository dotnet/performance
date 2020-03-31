using ScenarioMeasurement;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Startup.Tests
{
    public class StartupTests
    {
        Logger logger = new Logger("test-startup.log");
        string traceDirectory = Environment.CurrentDirectory;


        [WindowsOnly]
        public void TestWindowsTraceSession()
        {
            string sessionName = "test-windows-session";
            string traceName = "test-windows-trace";
            var session = new WindowsTraceSession(sessionName, traceName, traceDirectory, logger);
            var parser = new TimeToMainParser();
            TestSession(session, parser);
        }

        [LinuxOnly]
        public void TestLinuxTraceSession()
        {
            string sessionName = "test-linux-session";
            string traceName = "test-linux-trace";
            var session = new LinuxTraceSession(sessionName, traceName, traceDirectory, logger);
            var parser = new TimeToMainParser();
            TestSession(session, parser);
        }

        [WindowsOnly]
        public void TestProfileIteration()
        {
            string sessionName = "test-profile-iteration-session";
            string traceName = "test-profile-iteration-trace";
            var timeToMainParser = new TimeToMainParser();
            var profileParser = new ProfileParser(timeToMainParser);
            var profileSession = TraceSessionManager.CreateSession(sessionName, traceName, traceDirectory, logger);
            TestSession(profileSession, profileParser);
        }

        private void TestSession(ITraceSession session, IParser parser)
        {
            string traceFilePath = "";
            using (session)
            {
                session.EnableProviders(parser);
                Thread.Sleep(1);
                traceFilePath = session.TraceFilePath;
            }

            Assert.False(String.IsNullOrEmpty(traceFilePath));
            Assert.True(File.Exists(traceFilePath));
        }

        public sealed class WindowsOnly : FactAttribute
        {
            public WindowsOnly()
            {
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    Skip = "Skip on non-windows platform";
                }
            }
        }

        public sealed class LinuxOnly : FactAttribute
        {
            public LinuxOnly()
            {
                if(Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    Skip = "Skip on non-linux platform";
                }
            }
        }
    }
}
