using Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScenarioMeasurement
{
    class SizeOnDisk
    {
        /// <summary>
        /// SizeOnDisk reports breakdown information about file sizes under a directory (or set of directories)
        /// </summary>
        /// <param name="scenarioName">Scenario name for logging purposes</param>
        /// <param name="dirs">One or more directories to scan.</param>
        /// <param name="reportJsonPath">Output path for lab reporting purposes</param>
        /// <returns></returns>
        static int Main(string scenarioName,
                        string[] dirs,
                        string reportJsonPath = "")
        {
            if (string.IsNullOrEmpty(scenarioName) || dirs is null || dirs.Length == 0)
            {
                Console.WriteLine("Usage: SizeOnDisk <dir0> <dir1> <dirN>");
                return -1;
            }
            var directories = new Dictionary<string, Dictionary<string, long>>();

            foreach (var dir in dirs)
            {
                if(!Directory.Exists(dir))
                {
                    Console.WriteLine($"Directory {dir} does not exist");
                    return -1;
                }
                FindVersionNumbers(dir);
                directories.Add(dir, GetDirSize(dir));
            }

            var counters = new List<Counter>();
            long totalSize = 0;
            int totalCount = 0;
            bool directoryIsTop = dirs.Length > 1; // if we were asked to log more than one directory, include the summary info as top counters.
            foreach (var directory in directories)
            {
                var resultSize = directory.Value.Values.Sum();
                var resultCount = directory.Value.Values.Count();
                var name = RemoveVersions(directory.Key);
                totalSize += resultSize;
                totalCount += resultCount;
                counters.Add(new Counter { MetricName = "bytes", TopCounter = directoryIsTop, Name = $"{RemoveVersions(directory.Key)}", Results = new[] { (double)resultSize } });
                counters.Add(new Counter { MetricName = "count", TopCounter = directoryIsTop, Name = $"{RemoveVersions(directory.Key)}", Results = new[] { (double)resultCount } });
                foreach (var file in directory.Value)
                {
                    counters.Add(new Counter { MetricName = "bytes", Name = $"{RemoveVersions(file.Key)}", Results = new[] { (double)file.Value } });
                }
            }
            counters.Add(new Counter { MetricName = "bytes", Name = scenarioName, DefaultCounter = true, TopCounter = true, Results = new[] { (double)totalSize } });
            counters.Add(new Counter { MetricName = "count", Name = scenarioName, TopCounter = true, Results = new[] { (double)totalCount } });
            var reporter = Reporter.CreateReporter();
            if (reporter != null)
            {
                var test = new Test();
                test.Categories.Add("SizeOnDisk");
                test.Name = scenarioName;
                test.AddCounter(counters);
                reporter.AddTest(test);
                if (!String.IsNullOrEmpty(reportJsonPath))
                {
                    var json = reporter.GetJson();
                    if(json != null)
                    {
                        File.WriteAllText(reportJsonPath, json);
                    }
                }
            }
            Console.WriteLine(reporter.WriteResultTable());
            return 0;
        }

        static HashSet<string> versions = new HashSet<string>();

        static string RemoveVersions(string name)
        {
            foreach(var version in versions)
            {
                name = name.Replace(version, "VERSION");
            }
            return name;
        }

        static void FindVersionNumbers(string path)
        {
            var sdkDir = Path.Combine(path, "sdk");
            if (!Directory.Exists(sdkDir))
                return;
            var sharedDir = Path.Combine(path, "shared");
            var sdkVersion =  new DirectoryInfo(Directory.GetDirectories(sdkDir).Single()).Name;
            var templateDir = Path.Combine(path, "templates");

            versions.Add(sdkVersion);
            // the Templates dir seems to have the same version as the SDK version, but with a slightly different format
            versions.Add(Regex.Replace(sdkVersion, @"(\d+\.\d+\.\d)00", "$1"));

            versions.Add(new DirectoryInfo(Directory.GetDirectories(templateDir).Single()).Name);
            
            foreach(var dir in Directory.GetDirectories(sharedDir))
            {
                versions.Add(new DirectoryInfo(Directory.GetDirectories(dir).Single()).Name);
            }
        }

        static Dictionary<string, long> GetDirSize(string dir)
        {
            var ret = new Dictionary<string, long>();
            var dirInfo = new DirectoryInfo(dir);
            var options = new EnumerationOptions();
            options.AttributesToSkip ^= options.AttributesToSkip; // include hidden and system files
            options.RecurseSubdirectories = true;
            options.MatchType = MatchType.Win32;
            foreach (var fileInfo in dirInfo.EnumerateFiles("*", options))
            {
                ret.Add(fileInfo.FullName.Replace(dirInfo.FullName, ""), fileInfo.Length);
            }
            return ret;
        }
    }
}
