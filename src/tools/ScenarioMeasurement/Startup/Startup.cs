using Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ScenarioMeasurement;

enum MetricType
{
    TimeToMain,
    GenericStartup,
    ProcessTime,
    WPF,
    Crossgen2,
    InnerLoop,
    InnerLoopMsBuild,
    DotnetWatch,
    DeviceTimeToMain,
    PDN,
    WinUI,
    WinUIBlazor,
    TimeToMain2,
}

public class InnerLoopMarkerEventSource : EventSource
{
    public static InnerLoopMarkerEventSource Log = new();
    public void Split() => WriteEvent(1);
    public void EndIteration() => WriteEvent(2);
    public void DroppedFile() => WriteEvent(3);
}

class Startup
{
    private static IProcessHelper TestProcess { get; set; }
    /// <summary>
    ///
    /// </summary>
    /// <param name="appExe">Full path to test executable</param>
    /// <param name="metricType">Type of interval measurement</param>
    /// <param name="scenarioName">Scenario name for reporting</param>
    /// <param name="processWillExit">true: process exits on its own. False: process does not exit, send close.</param>
    /// <param name="timeout">Max wait for process to exit</param>
    /// <param name="measurementDelay">Allowed time for startup window</param>
    /// <param name="iterations">Number of measured iterations</param>
    /// <param name="appArgs">optional arguments to test executable</param>
    /// <param name="logFileName">optional log file. Default is appExe.startup.log</param>
    /// <param name="workingDir">optional working directory</param>
    /// <param name="warmup">enables/disables warmup iteration</param>
    /// <param name="traceName">trace name</param>
    /// <param name="guiApp">true: app under test is a GUI app, false: console</param>
    /// <param name="skipProfileIteration">true: skip full results iteration</param>
    /// <param name="reportJsonPath">path to save report json</param>
    /// <param name="iterationSetup">command to set up before each iteration</param>
    /// <param name="setupArgs">arguments of iterationSetup</param>
    /// <param name="iterationCleanup">command to clean up after each iteration</param>
    /// <param name="cleanupArgs">arguments of iterationCleanup</param>
    /// <param name="traceDirectory">Directory to put files in (defaults to current directory)</param>
    /// <param name="environmentVariables">Environment variables set for test processes (example: var1=value1;var2=value2)</param>
    /// <param name="innerLoopCommand">Command to be run between invocation one and two per iteration one and two</param>
    /// <param name="innerLoopCommandArgs">Args for command to be run between invocation one and two per iteration one and two</param>
    /// <param name="runWithoutExit">Run the main test process without handling shutdown</param>
    /// <param name="hotReloadIters">Number of times to change files for hot reload</param>
    /// <param name="skipMeasurementIteration">Don't run measurement collection</param>
    /// <param name="parseOnly">Parse trace(s) without running app</param>
    /// <param name="runWithDotnet">Run the app with dotnet, but don't include dotnet startup time</param>
    /// <param name="affinity">Processor affinity mask to set for the process</param>
    /// <returns></returns>

    static int Main(string appExe,
                    MetricType metricType,
                    string scenarioName,
                    string traceName,
                    bool processWillExit = false,
                    int iterations = 5,
                    string iterationSetup = "",
                    string setupArgs = "",
                    string iterationCleanup = "",
                    string cleanupArgs = "",
                    int timeout = 60,
                    int measurementDelay = 15,
                    string appArgs = "",
                    string logFileName = "",
                    string workingDir = "",
                    bool warmup = true,
                    bool guiApp = true,
                    bool skipProfileIteration = false,
                    string reportJsonPath = "",
                    string traceDirectory = null,
                    string environmentVariables = null,
                    string innerLoopCommand = "",
                    string innerLoopCommandArgs = "",
                    bool runWithoutExit = false,
                    int hotReloadIters = 1,
                    bool skipMeasurementIteration = false,
                    bool parseOnly = false,
                    bool runWithDotnet = false,
                    long affinity = 0
                    )
    {
        var logger = new Logger(string.IsNullOrEmpty(logFileName) ? $"{appExe}.startup.log" : logFileName);
        static void checkArg(string arg, string name)
        {
            if (string.IsNullOrEmpty(arg))
                throw new ArgumentException(name);
        };
        checkArg(appExe, nameof(appExe));
        checkArg(traceName, nameof(traceName));

        var traceFilePath = "";


        if (parseOnly == true)
        {
            skipMeasurementIteration = true;
            skipProfileIteration = true;
            traceFilePath = Path.Join(traceDirectory, traceName);
        }

        if (string.IsNullOrEmpty(traceDirectory))
        {
            traceDirectory = Environment.CurrentDirectory;
        }
        else
        {
            if (!Directory.Exists(traceDirectory))
            {
                Directory.CreateDirectory(traceDirectory);
            }
        }

        var envVariables = new Dictionary<string, string>(ParseStringToDictionary(environmentVariables));

        logger.Log($"Running {appExe} (args: \"{appArgs}\")");
        logger.Log($"Working Directory: {workingDir}");

        if (affinity > 0 && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
        {
            var currentProcessAffinity = (long)Process.GetCurrentProcess().ProcessorAffinity;
            if (affinity > currentProcessAffinity && currentProcessAffinity != -1) // -1 means all processors TODO: Check if there is a more proper way to deal with affinity for systems with more than 64 processors
            {
                throw new ArgumentException($"{nameof(affinity)} cannot be greater than the number of processors available to this process! (Current process affinity: {currentProcessAffinity}; Target affinity: {affinity})");
            }
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)affinity;
            currentProcessAffinity = (long)Process.GetCurrentProcess().ProcessorAffinity;
            logger.Log($"Process Affinity: {currentProcessAffinity}, mask: {Convert.ToString(currentProcessAffinity, 2)}");
        }
        else if (affinity != 0 && !(OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
        {
            throw new ArgumentException($"{nameof(affinity)} not supported on non-windows and non-linux platforms!");
        }
        else if (affinity < 0)
        {
            throw new ArgumentException($"{nameof(affinity)} cannot be negative!");
        }

        if (runWithoutExit)
        {
            TestProcess = new RawProcessHelper(logger)
            {
                ProcessWillExit = processWillExit,
                Timeout = timeout,
                MeasurementDelay = measurementDelay,
                Executable = appExe,
                Arguments = appArgs,
                WorkingDirectory = workingDir,
                GuiApp = guiApp,
                EnvironmentVariables = envVariables
            };
        }
        else
        {
            TestProcess = new ManagedProcessHelper(logger)
            {
                ProcessWillExit = processWillExit,
                Timeout = timeout,
                MeasurementDelay = measurementDelay,
                Executable = appExe,
                Arguments = appArgs,
                WorkingDirectory = workingDir,
                GuiApp = guiApp,
                EnvironmentVariables = envVariables,
                RunWithDotnet = runWithDotnet
            };
        }

        var secondTestProcess = metricType == MetricType.InnerLoop || metricType == MetricType.InnerLoopMsBuild ? TestProcess : null;

        //Create wait funcs for steady state and post-compilation
        Func<Process, string, bool> waitForSteadyState = metricType == MetricType.DotnetWatch ? (Proc, searchString) =>
        {
            var output = new StringBuilder();
            DataReceivedEventHandler stdOutProcessor = (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    Console.WriteLine(e.Data);
                }
            };
            DataReceivedEventHandler stdErrProcessor = (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            };
            Proc.OutputDataReceived += stdOutProcessor;
            Proc.ErrorDataReceived += stdErrProcessor;
            Proc.BeginOutputReadLine();
            Proc.BeginErrorReadLine();
            var isSteadyState = false;
            var timeoutCount = 0;
            while (!isSteadyState && timeoutCount < timeout)
            {
                foreach (var line in output.ToString().Split(Environment.NewLine))
                {
                    if (line.Contains(searchString))
                    {
                        isSteadyState = true;
                        break;
                    }
                }
                timeoutCount++;
                Thread.Sleep(1000);
            }
            Proc.CancelErrorRead();
            Proc.CancelOutputRead();
            Proc.ErrorDataReceived -= stdErrProcessor;
            Proc.OutputDataReceived -= stdOutProcessor;
            return true && timeoutCount < timeout;
        }
        : null;

        var waitForRecompile = waitForSteadyState;

        // create iteration setup process helper
        logger.Log($"Iteration set up: {iterationSetup} (args: {setupArgs})");
        IProcessHelper setupProcHelper = null;

        if (!string.IsNullOrEmpty(iterationSetup))
        {
            setupProcHelper = CreateProcHelper(iterationSetup, setupArgs, true, logger);
        }

        // create iteration cleanup process helper
        logger.Log($"Iteration clean up: {iterationCleanup} (args: {cleanupArgs})");
        IProcessHelper cleanupProcHelper = null;
        if (!string.IsNullOrEmpty(iterationCleanup))
        {
            cleanupProcHelper = CreateProcHelper(iterationCleanup, cleanupArgs, true, logger);
        }
        IProcessHelper innerLoopProcHelper = null;
        if (!string.IsNullOrEmpty(innerLoopCommand))
        {
            innerLoopProcHelper = CreateProcHelper(innerLoopCommand, innerLoopCommandArgs, true, logger);
        }

        Util.Init();

        // Warm up iteration
        if (warmup && !skipMeasurementIteration)
        {
            logger.LogIterationHeader("Warm up");
            if (!RunIteration(setupProcHelper, TestProcess, waitForSteadyState, innerLoopProcHelper, waitForRecompile, secondTestProcess, cleanupProcHelper, logger, hotReloadIters).Success)
            {
                return -1;
            }
        }

        IParser parser = metricType switch
        {
            MetricType.TimeToMain => new TimeToMainParser(),
            MetricType.GenericStartup => new GenericStartupParser(),
            MetricType.ProcessTime => new ProcessTimeParser(),
            MetricType.Crossgen2 => new Crossgen2Parser(),
            MetricType.WPF => new WPFParser(),
            MetricType.InnerLoop => new InnerLoopParser(processWillExit),
            MetricType.InnerLoopMsBuild => new InnerLoopMsBuildParser(),
            MetricType.DotnetWatch => new DotnetWatchParser(),
            MetricType.DeviceTimeToMain => new DeviceTimeToMain(),
            MetricType.PDN => new PDNStartupParser(),
            MetricType.WinUI => new WinUIParser(),
            MetricType.WinUIBlazor => new WinUIBlazorParser(),
            MetricType.TimeToMain2 => new TimeToMain2Parser(),
            _ => throw new ArgumentOutOfRangeException(),
        };

        var pids = new List<int>();
        var failed = false;

        if (!skipMeasurementIteration)
        {
            // Run trace session
            using (var traceSession = TraceSessionManager.CreateSession("StartupSession", traceName, traceDirectory, logger))
            {
                traceSession.EnableProviders(parser);
                for (var i = 0; i < iterations; i++)
                {
                    logger.LogIterationHeader($"Iteration {i}");
                    (bool Success, List<int> Pids) iterationResult;

                    iterationResult = RunIteration(setupProcHelper, TestProcess, waitForSteadyState, innerLoopProcHelper, waitForRecompile, secondTestProcess, cleanupProcHelper, logger, hotReloadIters);

                    if (!iterationResult.Success)
                    {
                        failed = true;
                        break;
                    }
                    pids.AddRange(iterationResult.Pids);
                }
                traceFilePath = traceSession.TraceFilePath;
            }

        }
        // Parse trace files
        if (!failed && !string.IsNullOrEmpty(traceFilePath))
        {
            logger.Log($"Parsing {traceFilePath}");

            if (guiApp)
            {
                appExe = Path.Join(workingDir, appExe);
            }
            var commandLine = $"\"{appExe}\"";
            if (!string.IsNullOrEmpty(appArgs))
            {
                commandLine = commandLine + " " + appArgs;
            }

            var processName = Path.GetFileNameWithoutExtension(appExe);
            try
            {
                var counters = parser.Parse(traceFilePath, processName, pids, commandLine);
                CreateTestReport(scenarioName, counters, reportJsonPath, logger);
            }
            catch
            {
                logger.Log($"{nameof(parser)} = {parser.GetType().FullName}");
                logger.Log($"{nameof(processName)} = {processName}");
                logger.Log($"{nameof(pids)} = {string.Join(", ", pids)}");
                logger.Log($"{nameof(commandLine)} = {commandLine}");
                var destFileName = Path.Join(Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT"), Path.GetFileName(traceFilePath));
                File.Copy(traceFilePath, destFileName, true);
                logger.Log($"Copied {traceFilePath} to {destFileName}");
                throw;
            }
        }

        // Skip unimplemented Linux profiling
        skipProfileIteration = skipProfileIteration || !TraceSessionManager.IsWindows;
        // Run profile session
        if (!failed && !skipProfileIteration)
        {
            logger.LogIterationHeader("Profile Iteration");
            var profiler = new ProfileParser(parser);
            (bool Success, List<int> Pids) iterationResult;
            using (var profileSession = TraceSessionManager.CreateSession("ProfileSession", "profile_" + traceName, traceDirectory, logger))
            {
                profileSession.EnableProviders(profiler);
                iterationResult = RunIteration(setupProcHelper, TestProcess, waitForSteadyState, innerLoopProcHelper, waitForRecompile, secondTestProcess, cleanupProcHelper, logger, hotReloadIters);

                if (!iterationResult.Success)
                {
                    failed = true;
                }
            }
        }

        return (failed ? -1 : 0);
    }


    private static IProcessHelper CreateProcHelper(string command, string args, bool runWithExit, Logger logger)
    {
        IProcessHelper procHelper;
        if (runWithExit)
        {
            procHelper = new ManagedProcessHelper(logger)
            {
                ProcessWillExit = true,
                Executable = command,
                Arguments = args,
                Timeout = 300
            };
        }
        else
        {
            procHelper = new RawProcessHelper(logger)
            {
                ProcessWillExit = true,
                Executable = command,
                Arguments = args,
                Timeout = 300
            };
        }
        return procHelper;
    }

    private static (bool Success, List<int> Pids) RunIteration(IProcessHelper setupHelper, IProcessHelper testHelper,
    Func<Process, string, bool> waitForSteadyState, IProcessHelper innerLoopProcHelper, Func<Process, string, bool> waitForRecompile,
    IProcessHelper secondTestHelper, IProcessHelper cleanupHelper, Logger logger, int hotReloadIters = 1)
    {
        (Process Proc, bool Success, int Pid) RunProcess(IProcessHelper helper)
        {
            var runResult = helper.Run();
            if (runResult.Pid != -1)
            {
                if (runResult.Result != Result.Success)
                {
                    logger.Log($"Process {runResult.Pid} failed to run. Result: {runResult.Result}");
                    return (null, false, runResult.Pid);
                }
                return (null, true, runResult.Pid);
            }
            else if (!runResult.Proc.HasExited)
            {
                return (runResult.Proc, true, -1);
            }
            else
            {
                return (runResult.Proc, false, -1);
            }
        }

        var pids = new List<int>();
        var failed = false;
        (Process Proc, bool Success, int Pid) runResult = default((Process p, bool Success, int Pid));

        if (setupHelper != null)
        {
            logger.LogStepHeader("Iteration Setup");
            failed = !RunProcess(setupHelper).Success;
        }

        // no need to run test process if setup failed
        if (!failed)
        {
            logger.LogStepHeader("Test");
            runResult = RunProcess(testHelper);
            failed = !runResult.Success;
            if (runResult.Pid == -1)
            {
                pids.Add(runResult.Proc.Id);
            }
            else
            {
                pids.Add(runResult.Pid);
            }
        }

        if (waitForSteadyState != null && !failed)
        {
            logger.LogStepHeader("Waiting for steady state");
            failed = failed || !waitForSteadyState(runResult.Proc, "Hot reload capabilities");
        }
        for (var i = 0; i < hotReloadIters; i++)
        {
            if (innerLoopProcHelper != null && !failed)
            {
                //Do some stuff to change the project
                logger.LogStepHeader("Inner Loop Setup");
                var innerLoopReturn = innerLoopProcHelper.Run();
                InnerLoopMarkerEventSource.Log.DroppedFile();
                if (innerLoopReturn.Result != Result.Success)
                {
                    failed = true;
                }
            }
            if (waitForRecompile != null && !failed)
            {
                logger.LogStepHeader("Waiting for recompile");
                failed = failed || !waitForRecompile(runResult.Proc, "Hot reload of changes succeeded");
            }
        }

        if (secondTestHelper != null && !failed)
        {
            var test = InnerLoopMarkerEventSource.GetSources();
            InnerLoopMarkerEventSource.Log.Split();

            logger.LogIterationHeader($"Iteration - Diff");
            var iterationResult = RunProcess(TestProcess);
            if (!iterationResult.Success)
            {
                failed = true;
            }
            InnerLoopMarkerEventSource.Log.EndIteration();
            if (iterationResult.Pid == -1)
            {
                pids.Add(iterationResult.Proc.Id);
            }
            else
            {
                pids.Add(iterationResult.Pid);
            }
        }
        if (runResult.Proc != null)
        {
            logger.LogStepHeader("Stopping process");
            runResult.Proc.Kill();
            Thread.Sleep(2000);
        }
        // need to clean up despite the result of setup and test
        if (cleanupHelper != null)
        {
            logger.LogStepHeader("Iteration Cleanup");
            failed = failed || !RunProcess(cleanupHelper).Success;
        }

        return (!failed, pids);
    }




    private static Dictionary<string, string> ParseStringToDictionary(string s)
    {
        var dict = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(s))
        {
            foreach (var substring in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = substring.Split('=');
                dict.Add(pair[0], pair[1]);
            }
        }
        return dict;
    }

    private static void CreateTestReport(string scenarioName, IEnumerable<Counter> counters, string reportJsonPath, Logger logger)
    {
        var reporter = Reporter.CreateReporter();
        var test = new Test();
        test.Categories.Add("Startup");
        test.Name = scenarioName;
        test.AddCounter(counters);
        reporter.AddTest(test);
        if (reporter.InLab && !string.IsNullOrEmpty(reportJsonPath))
        {
            File.WriteAllText(reportJsonPath, reporter.GetJson());
        }
        logger.Log(reporter.WriteResultTable());
    }

    public static void AddTestProcessEnvironmentVariable(string name, string value)
    {
        TestProcess.AddEnvironmentVariable(name, value);
    }
}
