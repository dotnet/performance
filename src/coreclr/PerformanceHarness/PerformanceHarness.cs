using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xunit.Performance.Api;

public class PerformanceHarness
{
    public static int Main(string[] args)
    {
        try
        {
            var harnessDirectoryInfo = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;
            IEnumerable<FileInfo> benchmarkAssemblies;
            var extensions = new HashSet<string> { ".dll", ".exe" };

            benchmarkAssemblies = (args.Length > 0 && IsValidAssembly(args[0])) ?
                (new[] { new FileInfo(args[0]) }).AsEnumerable() :
                harnessDirectoryInfo
                    .EnumerateFiles()
                    .Where(f => {
                            return f.Name.StartsWith("DotNetBenchmark-", StringComparison.OrdinalIgnoreCase) &&
                                extensions.Contains(f.Extension);
                        })
                    .OrderBy(benchmarkAssembly => benchmarkAssembly.Name);

            int exitCode = 0;
            using (var harness = new XunitPerformanceHarness(args))
            {
                foreach (var benchmarkAssemblyFileInfo in benchmarkAssemblies)
                {
                    try
                    {
                        harness.RunBenchmarks(assemblyFileName: benchmarkAssemblyFileInfo.FullName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[ERROR] Benchmark execution failed.");
                        Console.WriteLine($"  {ex.ToString()}");
                        ++exitCode;
                    }
                }
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Benchmark execution failed.");
            Console.WriteLine($"  {ex.ToString()}");
            return 1;
        }
    }

    private static bool IsValidAssembly(string fileName)
    {
        if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(fileName))
                return true;
            else
                throw new FileNotFoundException("Unable to find input file.", fileName);
        }

        return false;
    }
}
