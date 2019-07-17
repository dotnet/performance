﻿using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Reporting;
using System;
using System.IO;
using System.Linq;

namespace ScenarioMeasurement
{
    enum MetricType
    {
        TimeToMain,
        GenericStartup,
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
        /// <returns></returns>
        static int Main(string appExe,
                        MetricType metricType,
                        string scenarioName,
                        string traceFileName,
                        bool processWillExit = false,
                        int iterations = 5,
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
            var procHelper = new ProcessHelper()
            {
                ProcessWillExit = processWillExit,
                Timeout = timeout,
                MeasurementDelay = measurementDelay,
                Executable = appExe,
                Arguments = appArgs,
                WorkingDirectory = workingDir,
                GuiApp = guiApp
            };
            Util.Init();

            if (warmup)
            {
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
                    //case MetricType.WPF:
                    //    parser = new WPFParser();
                    //    break;
            }
            using (var kernel = new TraceEventSession(KernelTraceEventParser.KernelSessionName, kernelTraceFile))
            {
                parser.EnableKernelProvider(kernel);
                using (var user = new TraceEventSession("StartupSession", userTraceFile))
                {
                    parser.EnableUserProviders(user);
                    for (int i = 0; i < iterations; i++)
                    {
                        var result = procHelper.Run();
                        if (result != ProcessHelper.Result.Success)
                        {
                            logger.Log($"failed. result: {result}");
                            failed = true;
                            break;
                        }
                    }
                }
            }

            if (!failed)
            {
                logger.Log("Parsing..");
                TraceEventSession.Merge(new[] { kernelTraceFile, userTraceFile }, traceFileName);
                var counters = parser.Parse(traceFileName, Path.GetFileNameWithoutExtension(appExe));
                foreach (var counter in counters)
                {
                    logger.Log($"{counter.Name,-15}: {counter.Results.Average():F3} {counter.MetricName}");
                }

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
                        var result = procHelper.Run();
                        if (result != ProcessHelper.Result.Success)
                        {
                            logger.Log($"failed. result: {result}");
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
    }
}