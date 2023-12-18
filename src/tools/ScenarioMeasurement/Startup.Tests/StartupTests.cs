using Reporting;
using ScenarioMeasurement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;


namespace Startup.Tests;

public class StartupTests
{
    readonly Logger logger = new("test-startup.log");
    readonly string traceDirectory = Environment.CurrentDirectory;
    readonly string testAssetDirectory = "inputs";


    [WindowsOnly]
    public void TestWindowsTraceSession()
    {
        var sessionName = "test-windows-session";
        var traceName = "test-windows-trace";
        var session = new WindowsTraceSession(sessionName, traceName, traceDirectory, logger);
        var parser = new TimeToMainParser();
        TestSession(session, parser);
    }

    [LinuxOnly]
    public void TestLinuxTraceSession()
    {
        var sessionName = "test-linux-session";
        var traceName = "test-linux-trace";
        var session = new LinuxTraceSession(sessionName, traceName, traceDirectory, logger, ScenarioMeasurement.Startup.AddTestProcessEnvironmentVariable);
        var parser = new TimeToMainParser();
        TestSession(session, parser);
    }

    [WindowsOnly(Skip = "Skipping test until asset is provided")]
    public void TestProfileIteration()
    {
        var sessionName = "test-profile-iteration-session";
        var traceName = "test-profile-iteration-trace";
        var timeToMainParser = new TimeToMainParser();
        var profileParser = new ProfileParser(timeToMainParser);
        var profileSession = TraceSessionManager.CreateSession(sessionName, traceName, traceDirectory, logger, ScenarioMeasurement.Startup.AddTestProcessEnvironmentVariable);
        TestSession(profileSession, profileParser);
    }

    [Fact(Skip = "Skipping test until asset is provided")]
    public void TestProcessTimeParserLinux()
    {
        var ctfFile = Path.Combine(testAssetDirectory, "test-process-time_startup.trace.zip");
        var parser = new ProcessTimeParser();
        var pids = new List<int>() { 18627, 18674, 18721, 18768, 18813 };
        var counters = parser.Parse(ctfFile, "dotnet", pids, "\"dotnet\" build");
        var count = 0;
        foreach (var counter in counters)
        {
            Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
            count++;
        }
        Assert.True(count == 1, "Only Process Time counter should be present.");
    }

    [Fact(Skip = "Skipping test until asset is provided")]
    public void TestProcessTimeParserWindows()
    {
        var etlFile = Path.Combine(testAssetDirectory, "test-process-time_startup.etl");
        var parser = new ProcessTimeParser();
        var pids = new List<int>() { 32752, 6352, 16876, 10500, 17784 };
        var counters = parser.Parse(etlFile, "dotnet", pids, "\"dotnet\" build");
        var count = 0;
        foreach (var counter in counters)
        {
            Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
            count++;
        }
        Assert.True(count==2, "Both Process Time and Time On Thread counter should be present.");
    }

    [Fact(Skip = "Skipping test until asset is provided")]
    public void TestTimeToMainParserLinux()
    {
        var ctfFile = Path.Combine(testAssetDirectory, "test-time-to-main_startup.trace.zip");
        var parser = new TimeToMainParser();
        var pids = new List<int>() { 24352, 24362, 24371, 24380, 24389 };
        var counters = parser.Parse(ctfFile, "emptycsconsoletemplate", pids, "\"pub\\emptycsconsoletemplate.exe\"");
        var count = 0;
        foreach (var counter in counters)
        {
            Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
            count++;
        }
        Assert.True(count == 1, "Only Time To Main counter should be present.");
    }


    [Fact(Skip = "Skipping test until asset is provided")]
    public void TestTimeToMainParserWindows()
    {
        var etlFile = Path.Combine(testAssetDirectory, "test-time-to-main_startup.etl");
        var parser = new TimeToMainParser();
        var pids = new List<int>() { 17036, 21640, 12912, 19764, 11624 };
        var counters = parser.Parse(etlFile, "emptycsconsoletemplate", pids, "\"pub\\emptycsconsoletemplate.exe\"");
        var count = 0;
        foreach (var counter in counters)
        {
            Assert.True(counter.Results.Count == pids.Count, $"Counter {counter.Name} is expected to have {pids.Count} results.");
            count++;
        }
        Assert.True(count == 2, "Both Time To Main and Time On Thread counter should be present.");
    }


    private void TestSession(ITraceSession session, IParser parser)
    {
        var traceFilePath = "";
        using (session)
        {
            session.EnableProviders(parser);
            Thread.Sleep(1);
            traceFilePath = session.TraceFilePath;
        }

        Assert.False(string.IsNullOrEmpty(traceFilePath));
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
