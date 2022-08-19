using Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ScenarioMeasurement
{
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
        WinUIBlazor
    }

    public class InnerLoopMarkerEventSource : EventSource
    {
        public static InnerLoopMarkerEventSource Log = new InnerLoopMarkerEventSource();
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
                        bool runWithDotnet = false
                        )
        {
            Logger logger = new Logger(String.IsNullOrEmpty(logFileName) ? $"{appExe}.startup.log" : logFileName);
            static void checkArg(string arg, string name)
            {
                if (String.IsNullOrEmpty(arg))
                    throw new ArgumentException(name);
            };
            checkArg(appExe, nameof(appExe));
            checkArg(traceName, nameof(traceName));

            string traceFilePath = "";


            if (parseOnly == true)
            {
                skipMeasurementIteration = true;
                skipProfileIteration = true;
                traceFilePath = Path.Join(traceDirectory, traceName);
            }

            if (String.IsNullOrEmpty(traceDirectory))
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

            Dictionary<string, string> envVariables = new Dictionary<string, string>(ParseStringToDictionary(environmentVariables));

            logger.Log($"Running {appExe} (args: \"{appArgs}\")");
            logger.Log($"Working Directory: {workingDir}");
            if(runWithoutExit)
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

            IProcessHelper secondTestProcess = metricType == MetricType.InnerLoop || metricType == MetricType.InnerLoopMsBuild ? TestProcess : null;

            //Create wait funcs for steady state and post-compilation
            Func<Process, string, bool> waitForSteadyState = metricType == MetricType.DotnetWatch ? (Proc, searchString) =>
            {
                StringBuilder output = new StringBuilder();
                DataReceivedEventHandler stdOutProcessor = (s, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        Console.WriteLine(e.Data);
                    }
                };
                DataReceivedEventHandler stdErrProcessor = (s, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                };
                Proc.OutputDataReceived += stdOutProcessor;
                Proc.ErrorDataReceived += stdErrProcessor;
                Proc.BeginOutputReadLine();
                Proc.BeginErrorReadLine();
                bool isSteadyState = false;
                int timeoutCount = 0;
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

            Func<Process, string, bool> waitForRecompile = waitForSteadyState;

            // create iteration setup process helper
            logger.Log($"Iteration set up: {iterationSetup} (args: {setupArgs})");
            IProcessHelper setupProcHelper = null;

            if (!String.IsNullOrEmpty(iterationSetup))
            {
                setupProcHelper = CreateProcHelper(iterationSetup, setupArgs, true, logger);
            }

            // create iteration cleanup process helper
            logger.Log($"Iteration clean up: {iterationCleanup} (args: {cleanupArgs})");
            IProcessHelper cleanupProcHelper = null;
            if (!String.IsNullOrEmpty(iterationCleanup))
            {
                cleanupProcHelper = CreateProcHelper(iterationCleanup, cleanupArgs, true, logger);
            }
            IProcessHelper innerLoopProcHelper = null;
            if (!String.IsNullOrEmpty(innerLoopCommand))
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

            IParser parser = null;
            switch (metricType)
            {
                case MetricType.TimeToMain:
                    parser = new TimeToMainParser();
                    break;
                case MetricType.GenericStartup:
                    parser = new GenericStartupParser();
                    break;
                case MetricType.ProcessTime:
                    parser = new ProcessTimeParser();
                    break;
                case MetricType.Crossgen2:
                    parser = new Crossgen2Parser();
                    break;
                case MetricType.InnerLoop:
                    parser = new InnerLoopParser(processWillExit);
                    break;
                case MetricType.InnerLoopMsBuild:
                    parser = new InnerLoopMsBuildParser();
                    break;
                case MetricType.DotnetWatch:
                    parser = new DotnetWatchParser();
                    break;
                case MetricType.DeviceTimeToMain:
                    parser = new DeviceTimeToMain();
                    break;
                case MetricType.WPF:
                    parser = new WPFParser();
                    break;
                case MetricType.PDN:
                    parser = new PDNStartupParser();
                    break;
                case MetricType.WinUI:
                    parser = new WinUIParser();
                    break;
                case MetricType.WinUIBlazor:    
                    parser = new WinUIBlazorParser();
                    break;
            }

            var pids = new List<int>();
            bool failed = false;

            if(!skipMeasurementIteration) 
            {
                // Run trace session
                using (var traceSession = TraceSessionManager.CreateSession("StartupSession", traceName, traceDirectory, logger))
                {
                    traceSession.EnableProviders(parser);
                    for (int i = 0; i < iterations; i++)
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
                string commandLine = $"\"{appExe}\"";
                if (!String.IsNullOrEmpty(appArgs))
                {
                    commandLine = commandLine + " " + appArgs;
                }

                var counters = parser.Parse(traceFilePath, Path.GetFileNameWithoutExtension(appExe), pids, commandLine);

                CreateTestReport(scenarioName, counters, reportJsonPath, logger);
            }

            // Skip unimplemented Linux profiling
            skipProfileIteration = skipProfileIteration || !TraceSessionManager.IsWindows;
            // Run profile session
            if (!failed && !skipProfileIteration)
            {
                logger.LogIterationHeader("Profile Iteration");
                ProfileParser profiler = new ProfileParser(parser);
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
            if(runWithExit)
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

            List<int> pids = new List<int>();
            bool failed = false;
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
                if(runResult.Pid == -1)
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
            for(int i = 0; i < hotReloadIters; i++)
            {
                if(innerLoopProcHelper != null  && !failed)
                {
                    //Do some stuff to change the project
                    logger.LogStepHeader("Inner Loop Setup");
                    var innerLoopReturn = innerLoopProcHelper.Run();
                    InnerLoopMarkerEventSource.Log.DroppedFile();
                    if(innerLoopReturn.Result != Result.Success)
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
            
            if (secondTestHelper != null  && !failed)
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
                if(iterationResult.Pid == -1)
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
            if (!String.IsNullOrEmpty(s))
            {
                foreach (string substring in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
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
            if (reporter.InLab && !String.IsNullOrEmpty(reportJsonPath))
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


}
