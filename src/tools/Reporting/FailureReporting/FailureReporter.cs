using Reporting;
using System;
using System.IO;

namespace FailureReporting
{
    public class FailureReporter
    {
        public static void Main(string[] args)
        {
            CreateFailureReport(args[0]);
        }

        public static void CreateFailureReport(string reportJsonPath)
        {
            var reporter = Reporter.CreateReporter();
            if (reporter.InLab && !String.IsNullOrEmpty(reportJsonPath))
            {
                File.WriteAllText(reportJsonPath, reporter.GetJson());
            }
        }
    }
}
