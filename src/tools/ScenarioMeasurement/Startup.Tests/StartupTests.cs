using Reporting;
using ScenarioMeasurement;
using System;
using System.IO;
using System.Resources;
using Xunit;

namespace Startup.Tests
{
    public class StartupTests
    {
        Logger logger = new Logger("test-startup.log");
        string traceDirectory = Environment.CurrentDirectory;

        [Fact]
        public void TestWindowsTraceSession()
        {
            string sessionName = "test-windows-session";
            string traceName = "test-windows-trace";
            using(var session = new WindowsTraceSession(sessionName, traceName, traceDirectory, logger))
            {
                var parser = new ProcessTimeParser();
                session.EnableProviders(parser);
                RunTestIteration();
            }
        }

        [Fact]
        public void TestLinuxTraceSession()
        {
            string sessionName = "test-linux-session";
            string traceName = "test-linux-trace";
            using(var session = new LinuxTraceSession(sessionName, traceName, traceDirectory, logger))
            {
                var parser = new ProcessTimeParser();
                session.EnableProviders(parser);
                RunTestIteration();
            }
        }

        [Fact]
        public void TestProfileIteration()
        {
            string sessionName = "test-profile-iteration-session";
            string traceName = "test-profile-iteration-trace";

            var processTimeParser = new ProcessTimeParser();
            var profileParser = new ProfileParser(processTimeParser);
            using(var profileSession = TraceSessionManager.CreateProfileSession(sessionName, traceName, traceDirectory, logger))
            {
                profileSession.EnableProviders(profileParser);
                RunTestIteration();
            }
        }

        private void RunTestIteration()
        {
            var procHelper = new ProcessHelper(logger) {
                Executable = "dotnet",
                Arguments = "--info",
                ProcessWillExit = true,
                GuiApp = false
            };

        }
    }
}
