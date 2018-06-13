// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xunit.Performance.Api;

public class PerfHarness
{
    public static int Main(string[] args)
    {
        try
        {
            using (XunitPerformanceHarness harness = new XunitPerformanceHarness(args))
            {
                foreach(var testName in GetTestAssemblies(args))
                {
                    Console.WriteLine("Running " + testName.FullName);
                    try
                    {
                        harness.RunBenchmarks(GetTestAssembly(testName.FullName));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[ERROR] Benchmark execution failed.");
                        Console.WriteLine($"  {ex.ToString()}");
                        return 1;
                    }
                    
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Benchmark execution failed.");
            Console.WriteLine($"  {ex.ToString()}");
            return 1;
        }
    }

    private static string GetTestAssembly(string testName)
    {
        // Assume test assemblies are colocated/restored next to the PerfHarness.
        return Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), testName);
    }

    private static IEnumerable<FileInfo> GetTestAssemblies(string[] args)
    {
        foreach(string s in args)
        {
            Console.WriteLine(s);
        }
        var benchmarkLocation = (args.Length > 0 && IsValidAssembly(args[0])) ?
        (new[] { new FileInfo(args[0]) }).AsEnumerable() :
        (new FileInfo(Assembly.GetEntryAssembly().Location).Directory).EnumerateFiles()
                    .Where(f => {return f.Name.EndsWith("Performance.Tests.dll", StringComparison.OrdinalIgnoreCase);})
                    .OrderBy(benchmarkAssembly => benchmarkAssembly.Name);
        return benchmarkLocation;
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
