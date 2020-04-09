using Reporting;
using ScenarioMeasurement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace Startup.Tests
{
    public class StartupTests
    {
        Logger logger = new Logger("test-startup.log");
        string traceDirectory = Environment.CurrentDirectory;
        string testAssetDirectory = "inputs";


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

        [Fact]
        public void TestProcessTimeParserLinux()
        {
            string ctfFile = Path.Combine(testAssetDirectory, "test-process-time_startup.trace.zip");
            var parser = new ProcessTimeParser();
            var pids = new List<int>() { 18627, 18674, 18721, 18768, 18813 };
            IEnumerable<Counter> counters = parser.Parse(ctfFile, "dotnet", pids, "\"dotnet\" build");
            int count = 0;
            foreach (var counter in counters)
            {
                Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
                count++;
            }
            Assert.True(count == 1, "Only Process Time counter should be present.");
        }

        [Fact]
        public void TestProcessTimeParserWindows()
        {
            string etlFile = Path.Combine(testAssetDirectory, "test-process-time_startup.etl");
            var parser = new ProcessTimeParser();
            var pids = new List<int>() { 32752, 6352, 16876, 10500, 17784 };
            IEnumerable<Counter> counters = parser.Parse(etlFile, "dotnet", pids, "\"dotnet\" build");
            int count = 0;
            foreach (var counter in counters)
            {
                Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
                count++;
            }
            Assert.True(count==2, "Both Process Time and Time To Main counter should be present.");
        }

        [Fact]
        public void TestTimeToMainParserLinux()
        {
            string ctfFile = Path.Combine(testAssetDirectory, "test-time-to-main_startup.trace.zip");
            var parser = new TimeToMainParser();
            var pids = new List<int>() { 24352, 24362, 24371, 24380, 24389 };
            IEnumerable<Counter> counters = parser.Parse(ctfFile, "emptycsconsoletemplate", pids, "\"pub\\emptycsconsoletemplate.exe\"");
            int count = 0;
            foreach (var counter in counters)
            {
                Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
                count++;
            }
            Assert.True(count == 2, "Only Time To Main counter should be present.");
        }


        [Fact]
        public void TestTimeToMainParserWindows()
        {
            string etlFile = Path.Combine(testAssetDirectory, "test-time-to-main_startup.etl");
            var parser = new TimeToMainParser();
            var pids = new List<int>() { 17036, 21640, 12912, 19764, 11624 };
            IEnumerable<Counter> counters = parser.Parse(etlFile, "emptycsconsoletemplate", pids, "\"pub\\emptycsconsoletemplate.exe\"");
            int count = 0;
            foreach (var counter in counters)
            {
                Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
                count++;
            }
            Assert.True(count == 2, "Both Time To Main and Time To Thread counter should be present.");
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
