using Reporting;
using System;
using System.IO;

namespace FailureReporting
{
    public class FailureReporter
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("FailureReporting.exe was called without the required JSON path argument");
            }
            else if (args.Length > 1)
            {
                throw new ArgumentException("FailureReporting.exe was called with more than one argument");
            }
            else if (!Directory.Exists(Path.GetDirectoryName(args[0])))
            {
                throw new IOException($"Provided directory {Path.GetDirectoryName(args[0])} for JSON output does not exist");
            }
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
