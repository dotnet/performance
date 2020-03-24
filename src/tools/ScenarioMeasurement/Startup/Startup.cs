using Microsoft.Diagnostics.Tracing.Parsers;
using Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ScenarioMeasurement
{
    enum MetricType
    {
        TimeToMain,
        GenericStartup,
        ProcessTime,
        WPF
    }
    class Startup
    {
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
        /// <param name="traceFileName">trace file name</param>
        /// <param name="guiApp">true: app under test is a GUI app, false: console</param>
        /// <param name="skipProfileIteration">true: skip full results iteration</param>
        /// <param name="reportJsonPath">path to save report json</param>
        /// <param name="iterationSetup">command to set up before each iteration</param>
        /// <param name="setupArgs">arguments of iterationSetup</param>
        /// <param name="iterationCleanup">command to clean up after each iteration</param>
        /// <param name="cleanupArgs">arguments of iterationCleanup</param>
        /// <param name="traceDirectory">Directory to put files in (defaults to current directory)</param>
        /// <param name="environmentVariables">Environment variables set for test processes (example: var1=value1;var2=value2)</param>
        /// <returns></returns>

        static int Main(string appExe,
                        MetricType metricType,
                        string scenarioName,
                        string traceFileName,
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
                        string environmentVariables = null
                        )
        {
            Logger logger = new Logger(String.IsNullOrEmpty(logFileName) ? $"{appExe}.startup.log" : logFileName);
            static void checkArg(string arg, string name)
            {
                if (String.IsNullOrEmpty(arg))
                    throw new ArgumentException(name);
            };
            checkArg(appExe, nameof(appExe));
            checkArg(traceFileName, nameof(traceFileName));

            if (String.IsNullOrEmpty(traceDirectory))
            {
                traceDirectory = Environment.CurrentDirectory;
            }
            else
            {
                if(!Directory.Exists(traceDirectory))
                {
                    Directory.CreateDirectory(traceDirectory);
                }
            }

            Dictionary<string, string> envVariables = null;
            if (!String.IsNullOrEmpty(environmentVariables))
            {
                envVariables = ParseStringToDictionary(environmentVariables);
            }

            logger.Log($"Running {appExe} (args: \"{appArgs}\")");
            logger.Log($"Working Directory: {workingDir}");
            var procHelper = new ProcessHelper(logger)
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
            
            // create iteration setup process helper
            logger.Log($"Iteration set up: {iterationSetup} (args: {setupArgs})");
            ProcessHelper setupProcHelper = null;
            if (!String.IsNullOrEmpty(iterationSetup))
            {
                setupProcHelper = CreateProcHelper(iterationSetup, setupArgs, logger);
            }

            // create iteration cleanup process helper
            logger.Log($"Iteration clean up: {iterationCleanup} (args: {cleanupArgs})");
            ProcessHelper cleanupProcHelper = null;
            if (!String.IsNullOrEmpty(iterationCleanup))
            {
                cleanupProcHelper = CreateProcHelper(iterationCleanup, cleanupArgs, logger);
            }

            Util.Init();

            // Warm up iteration
            if (warmup)
            {
                logger.LogIterationHeader("Warm up");
                if (!RunIteration(setupProcHelper, procHelper, cleanupProcHelper, logger).Success)
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
                    //case MetricType.WPF:
                    //    parser = new WPFParser();
                    //    break;
            }

            var pids = new List<int>();
            bool failed = false;
            string traceFilePath = "";

            // Run trace session
            using (var traceSession = TraceSessionManager.CreateSession("StartupSession", traceFileName, traceDirectory, logger))
            {
                traceSession.EnableProviders(parser);
                for (int i = 0; i < iterations; i++)
                {
                    logger.LogIterationHeader($"Iteration {i}");
                    var iterationResult = RunIteration(setupProcHelper, procHelper, cleanupProcHelper, logger);
                    if (!iterationResult.Success)
                    {
                        failed = true;
                        break;
                    }
                    pids.Add(iterationResult.Pid);
                }
                traceFilePath = traceSession.GetTraceFilePath();
            }

            // Parse trace files
            if (!failed)
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

                WriteResultTable(counters, logger);

                CreateTestReport(scenarioName, counters, reportJsonPath);
            }

            // Run profile session
            if (!failed && !skipProfileIteration)
            {
                logger.LogIterationHeader("Profile Iteration");
                ProfileParser profiler = new ProfileParser(parser);
                using (var profileSession = TraceSessionManager.CreateProfileSession("ProfileSession", "profile_"+traceFileName, traceDirectory, logger))
                {
                    profileSession.EnableProviders(profiler);
                    if (!RunIteration(setupProcHelper, procHelper, cleanupProcHelper, logger).Success)
                    {
                        failed = true;
                    }
                }
            }

            return (failed ? -1 : 0);
        }


        private static ProcessHelper CreateProcHelper(string command, string args, Logger logger)
        {
            var procHelper = new ProcessHelper(logger)
            {
                ProcessWillExit = true,
                Executable = command,
                Arguments = args,
                Timeout = 300
            };
            return procHelper;
        }

        private static (bool Success, int Pid) RunIteration(ProcessHelper setupHelper, ProcessHelper testHelper, ProcessHelper cleanupHelper, Logger logger)
        {
            (bool Success, int Pid) RunProcess(ProcessHelper helper)
            {
                var runResult = helper.Run();
                if (runResult.Result != ProcessHelper.Result.Success)
                {
                    logger.Log($"Process {runResult.Pid} failed to run. Result: {runResult.Result}"); 
                    return (false, runResult.Pid); 
                }
                return (true, runResult.Pid); 
            }

            bool failed = false;
            int pid = 0;
            if (setupHelper != null)
            {
                logger.LogStepHeader("Iteration Setup");
                failed = !RunProcess(setupHelper).Success;
            }

            // no need to run test process if setup failed
            if (!failed)
            {
                logger.LogStepHeader("Test");
                var testProcessResult = RunProcess(testHelper);
                failed = !testProcessResult.Success;
                pid = testProcessResult.Pid;
            }

            // need to clean up despite the result of setup and test
            if (cleanupHelper != null)
            {
                logger.LogStepHeader("Iteration Cleanup");
                failed = failed || !RunProcess(cleanupHelper).Success;
            }

            return (!failed, pid);
        }


        private static void WriteResultTable(IEnumerable<Counter> counters, Logger logger)
        {
            logger.Log($"{"Metric",-15}|{"Average",-15}|{"Min",-15}|{"Max",-15}");
            logger.Log($"---------------|---------------|---------------|---------------");
            foreach (var counter in counters)
            {
                string average = $"{counter.Results.Average():F3} {counter.MetricName}";
                string max = $"{counter.Results.Max():F3} {counter.MetricName}";
                string min = $"{counter.Results.Min():F3} {counter.MetricName}";
                logger.Log($"{counter.Name,-15}|{average,-15}|{min,-15}|{max,-15}");
            }
        }

        private static Dictionary<string, string> ParseStringToDictionary(string s)
        {
            var dict = new Dictionary<string, string>();
            foreach (string substring in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = substring.Split('=');
                dict.Add(pair[0], pair[1]);
            }
            return dict;
        }

        private static void CreateTestReport(string scenarioName, IEnumerable<Counter> counters, string reportJsonPath)
        {
            var reporter = Reporter.CreateReporter();
            if (reporter != null)
            {
                var test = new Test();
                test.Categories.Add("Startup");
                test.Name = scenarioName;
                test.AddCounter(counters);
                reporter.AddTest(test);
                if (!String.IsNullOrEmpty(reportJsonPath))
                {
                    File.WriteAllText(reportJsonPath, reporter.GetJson());
                }
            }
        }
    }

 
}
