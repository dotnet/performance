using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        /// <param name="iterations">Number of measured iterations</param>
        /// <param name="timeout">Max wait for process to exit</param>
        /// <param name="measurementDelay">Allowed time for startup window</param>
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
                        string reportJsonPath = "")
        {
            Logger logger = new Logger(String.IsNullOrEmpty(logFileName) ? $"{appExe}.startup.log" : logFileName);
            static void checkArg(string arg, string name)
            {
                if (String.IsNullOrEmpty(arg))
                    throw new ArgumentException(name);
            };
            checkArg(scenarioName, nameof(scenarioName));
            checkArg(appExe, nameof(appExe));
            checkArg(traceFileName, nameof(traceFileName));

            bool failed = false;
            logger.Log($"Running {appExe} (args: \"{appArgs}\")");
            var procHelper = new ProcessHelper(logger)
            {
                ProcessWillExit = processWillExit,
                Timeout = timeout,
                MeasurementDelay = measurementDelay,
                Executable = appExe,
                Arguments = appArgs,
                WorkingDirectory = workingDir,
                GuiApp = guiApp
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

            if (warmup)
            {
                logger.Log("=============== Warm up ================");
                procHelper.Run();
            }

            string kernelTraceFile = Path.ChangeExtension(traceFileName, "perflabkernel.etl");
            string userTraceFile = Path.ChangeExtension(traceFileName, "perflabuser.etl");
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
            using (var kernel = new TraceEventSession(KernelTraceEventParser.KernelSessionName, kernelTraceFile))
            {
                parser.EnableKernelProvider(kernel);
                using (var user = new TraceEventSession("StartupSession", userTraceFile))
                {
                    parser.EnableUserProviders(user);
                    for (int i = 0; i < iterations; i++)
                    {
                        logger.Log($"=============== Iteration {i} ================ ");
                        // set up iteration
                        if (setupProcHelper != null)
                        {
                            var setupResult = setupProcHelper.Run().result;
                            if (setupResult != ProcessHelper.Result.Success)
                            {
                                logger.Log($"Failed to set up. Result: {setupResult}");
                                failed = true;
                                break;
                            }
                        }

                        // run iteration
                        var runResult = procHelper.Run();
                        if (runResult.result == ProcessHelper.Result.Success)
                        {
                            pids.Add(runResult.pid);
                        }
                        else
                        {
                            logger.Log($"Failed to run. Result: {runResult.result}");
                            failed = true;
                            break;
                        }

                        // clean up iteration
                        if (cleanupProcHelper != null)
                        {
                            var cleanupResult = cleanupProcHelper.Run().result;
                            if (cleanupResult != ProcessHelper.Result.Success)
                            {
                                logger.Log($"Failed to clean up. Result: {cleanupResult}");
                                failed = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!failed)
            {
                logger.Log("Parsing..");
                var files = new List<string> { kernelTraceFile };
                if (File.Exists(userTraceFile))
                {
                    files.Add(userTraceFile);
                }
                TraceEventSession.Merge(files.ToArray(), traceFileName);

                string commandLine = $"\"{appExe}\" {appArgs}";
                var counters = parser.Parse(traceFileName, Path.GetFileNameWithoutExtension(appExe), pids, commandLine);

                WriteResultTable(counters, logger);

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

            File.Delete(kernelTraceFile);
            File.Delete(userTraceFile);

            if (!skipProfileIteration)
            {
                string profileTraceFileName = $"{Path.GetFileNameWithoutExtension(traceFileName)}_profile.etl";
                string profileKernelTraceFile = Path.ChangeExtension(profileTraceFileName, ".kernel.etl");
                string profileUserTraceFile = Path.ChangeExtension(profileTraceFileName, ".user.etl");
                ProfileParser profiler = new ProfileParser(parser);
                using (var kernel = new TraceEventSession(KernelTraceEventParser.KernelSessionName, profileKernelTraceFile))
                {
                    profiler.EnableKernelProvider(kernel);
                    using (var user = new TraceEventSession("ProfileSession", profileUserTraceFile))
                    {
                        profiler.EnableUserProviders(user);
                        var result = procHelper.Run().result;
                        if (result != ProcessHelper.Result.Success)
                        {
                            logger.Log($"Failed. Result: {result}");
                            failed = true;
                        }
                    }
                }
                if (!failed)
                {
                    logger.Log("Merging profile..");
                    TraceEventSession.Merge(new[] { profileKernelTraceFile, profileUserTraceFile }, profileTraceFileName);
                }
                File.Delete(profileKernelTraceFile);
                File.Delete(profileUserTraceFile);
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
    }
}
