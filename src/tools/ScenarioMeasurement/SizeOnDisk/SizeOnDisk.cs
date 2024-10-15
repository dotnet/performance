using Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScenarioMeasurement;

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
            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"Directory {dir} does not exist");
                return -1;
            }
            FindVersionNumbers(dir);
            directories.Add(dir, GetDirSize(dir));
        }

        var counters = new List<Counter>();
        long totalSize = 0;
        var totalCount = 0;
        var directoryIsTop = dirs.Length > 1; // if we were asked to log more than one directory, include the summary info as top counters.
        var buckets = new Dictionary<string, (long size, int count, bool isTop)>();
        foreach (var directory in directories)
        {
            var resultSize = directory.Value.Values.Sum();
            var resultCount = directory.Value.Values.Count();
            var name = RemoveVersions(directory.Key);
            totalSize += resultSize;
            totalCount += resultCount;
            counters.Add(new Counter { MetricName = "bytes", TopCounter = directoryIsTop, Name = name, Results = new[] { (double)resultSize } });
            counters.Add(new Counter { MetricName = "count", TopCounter = directoryIsTop, Name = $"{name} - Count", Results = new[] { (double)resultCount } });
            foreach (var file in directory.Value)
            {
                var fileName = RemoveVersions(file.Key);
                var fileExtension = GetExtension(fileName);
                var counterName = Path.Join(name, fileName);
                counters.Add(new Counter { MetricName = "bytes", Name = $"{counterName}", Results = new[] { (double)file.Value } });

                AddToBucket(buckets, $"Aggregate - {fileExtension}", file.Value);
                if (fileName.Contains(Path.Join("wwwroot", "_framework")))
                {
                    AggregateBlazorCounters(buckets, fileExtension, file.Value);
                }
                if (fileExtension.Equals(".mstat", StringComparison.OrdinalIgnoreCase))
                {
                    var processor = new MStatProcessor();
                    var fullName = Path.Join(directory.Key, file.Key);
                    processor.Process(fullName);
                    foreach (var item in processor.AssemblyStats)
                    {
                        counters.Add(new Counter { MetricName = "bytes", Name = $"{counterName} - Assembly - {item.Name}", Results = new[] { (double)item.Size } });
                    }
                    foreach (var item in processor.BlobStats)
                    {
                        counters.Add(new Counter { MetricName = "bytes", Name = $"{counterName} - Blob - {item.Name}", Results = new[] { (double)item.Size } });
                    }
                    totalSize -= new FileInfo(fullName).Length;
                    totalCount -= 1;
                }
            }
        }

        foreach (var bucket in buckets)
        {
            counters.Add(new Counter { MetricName = "bytes", TopCounter = bucket.Value.isTop, Name = bucket.Key, Results = new[] { (double)bucket.Value.size } });
            counters.Add(new Counter { MetricName = "count", TopCounter = bucket.Value.isTop, Name = $"{bucket.Key} - Count", Results = new[] { (double)bucket.Value.count } });
        }
        counters.Add(new Counter { MetricName = "bytes", Name = scenarioName, DefaultCounter = true, TopCounter = true, Results = new[] { (double)totalSize } });
        counters.Add(new Counter { MetricName = "count", Name = $"{scenarioName} - Count", TopCounter = true, Results = new[] { (double)totalCount } });


        var reporter = new Reporter();
        if (reporter != null)
        {
            var test = new Test();
            test.Categories.Add("SizeOnDisk");
            test.Name = scenarioName;
            test.AddCounters(counters);
            reporter.AddTest(test);
            if (reporter.InLab && !string.IsNullOrEmpty(reportJsonPath))
            {
                File.WriteAllText(reportJsonPath, reporter.GetJson());
            }
        }
        Console.WriteLine(reporter.WriteResultTable());
        return 0;
    }

    private static string GetExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "No Extension";
        }
        return extension;
    }

    static readonly HashSet<string> versions = new();

    static string RemoveVersions(string name)
    {
        foreach (var version in versions)
        {
            name = name.Replace(version, "VERSION");
        }
        return name;
    }

    static void FindVersionNumbers(string path)
    {
        var sdkDir = Path.Combine(path, "sdk");
        if (Directory.Exists(sdkDir))
        {
            var sharedDir = Path.Combine(path, "shared");
            var sdkVersion = new DirectoryInfo(Directory.GetDirectories(sdkDir).Single()).Name;
            var templateDir = Path.Combine(path, "templates");
            versions.Add(sdkVersion);
            // the Templates dir seems to have the same version as the SDK version, but with a slightly different format
            versions.Add(Regex.Replace(sdkVersion, @"(\d+\.\d+\.\d)00", "$1"));
            versions.Add(new DirectoryInfo(Directory.GetDirectories(templateDir).Single()).Name);
            foreach (var dir in Directory.GetDirectories(sharedDir))
            {
                versions.Add(new DirectoryInfo(Directory.GetDirectories(dir).Single()).Name);
            }
        }

        foreach (string wasmFile in Directory.GetFiles(path, "dotnet.*.js.*", SearchOption.AllDirectories))
        {
            var groups = Regex.Match(wasmFile, @"dotnet.(?:native|runtime).\d\.\d\..+-(?<versionWithHash>.+)\.js").Groups;
            // e.g. dotnet.native.8.0.0-rc.1.23375.3.trarwnmzt5.js -> rc.1.23375.3.trarwnmzt5
            string versionWithHash = groups["versionWithHash"].Value;
            if (!string.IsNullOrEmpty(versionWithHash))
                versions.Add(versionWithHash);
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


    static void AddToBucket(Dictionary<string, (long size, int count, bool isTop)> buckets, string bucketName, long size, bool isTop = false)
    {
        if (!buckets.ContainsKey(bucketName))
        {
            buckets.Add(bucketName, (0, 0, false));
        }
        var bucket = buckets[bucketName];
        bucket.size += size;
        bucket.count++;
        bucket.isTop = isTop;
        buckets[bucketName] = bucket;
    }

    static void AggregateBlazorCounters(Dictionary<string, (long size, int count, bool isTop)> buckets, string extension, long size)
    {
        if (extension == ".br")
        {
            AddToBucket(buckets, "Synthetic Wire Size - .br", size, true);
        }
        else if (extension == ".gz")
        {
            AddToBucket(buckets, "Synthetic Wire Size - .gz", size, true);
        }
        else
        {
            AddToBucket(buckets, "Total Uncompressed _framework", size, true);
        }
    }
}
