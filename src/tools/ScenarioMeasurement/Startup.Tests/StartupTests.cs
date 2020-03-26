using Microsoft.DotNet.PlatformAbstractions;
using Reporting;
using ScenarioMeasurement;
using System;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Resources;
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

        [Fact]
        public void TestProfileIteration()
        {
            string sessionName = "test-profile-iteration-session";
            string traceName = "test-profile-iteration-trace";
            var timeToMainParser = new TimeToMainParser();
            var profileParser = new ProfileParser(timeToMainParser);
            var profileSession = TraceSessionManager.CreateProfileSession(sessionName, traceName, traceDirectory, logger);
            TestSession(profileSession, profileParser);
        }

        private void TestSession(ITraceSession session, IParser parser)
        {
            ProcessHelper.Result result;
            string traceFilePath = "";
            using (session)
            {
                session.EnableProviders(parser);
                result = RunTestIteration();
                traceFilePath = session.GetTraceFilePath();
            }
            if (result != ProcessHelper.Result.Success)
            {
                throw new Exception("Test iteration failed.");
            }
            Assert.True(!String.IsNullOrEmpty(traceFilePath) && File.Exists(traceFilePath));
        }

        private ProcessHelper.Result RunTestIteration()
        {
            var procHelper = new ProcessHelper(logger) {
                Executable = "dotnet",
                Arguments = "--info",
                ProcessWillExit = true,
                GuiApp = false
            };
            return procHelper.Run().Result;
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
