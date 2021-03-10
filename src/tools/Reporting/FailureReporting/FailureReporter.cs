using Reporting;
using System;
using System.Collections.Generic;
using System.IO;

namespace FailureReporting
{
    public class FailureReporter
    {
        public static void CreateFailureReport(string reportJsonPath, Logger logger)
        {
            var reporter = Reporter.CreateReporter();
            if (reporter.InLab && !String.IsNullOrEmpty(reportJsonPath))
            {
                File.WriteAllText(reportJsonPath, reporter.GetJson());
            }
            logger.Log(reporter.WriteResultTable());
        }

        public class Logger
        {
            public Logger(string fileName)
            {

            }
            public void Log(string message)
            {
                Console.WriteLine(message);
            }

            public void LogIterationHeader(string message)
            {
                Console.WriteLine($"=============== {message} ================ ");
            }

            public void LogStepHeader(string message)
            {
                Console.WriteLine($"***{message}***");
            }

            public void LogVerbose(string message)
            {
                Console.WriteLine(message);
            }
        }
    }
}
