// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace Reporting;

public class Reporter
{
    private Run run;
    private Os os;
    private Build build;
    private readonly List<Test> tests = new();
    protected IEnvironment environment;

    private Reporter() { }

    public void AddTest(Test test)
    {
        if (tests.Any(t => t.Name.Equals(test.Name)))
            throw new Exception($"Duplicate test name, {test.Name}");
        tests.Add(test);
    }

    /// <summary>
    /// Get a Reporter. Relies on environment variables.
    /// </summary>
    /// <param name="environment">Optional environment variable provider</param>
    /// <returns>A Reporter instance or null if the environment is incorrect.</returns>
    public static Reporter CreateReporter(IEnvironment environment = null)
    {
        var ret = new Reporter();
        ret.environment = environment == null ? new EnvironmentProvider() : environment;
        if (ret.InLab)
        {
            ret.Init();
        }

        return ret;
    }

    private void Init()
    {
        run = new Run
        {
            CorrelationId = environment.GetEnvironmentVariable("HELIX_CORRELATION_ID"),
            PerfRepoHash = environment.GetEnvironmentVariable("PERFLAB_PERFHASH"),
            Name = environment.GetEnvironmentVariable("PERFLAB_RUNNAME"),
            Queue = environment.GetEnvironmentVariable("PERFLAB_QUEUE"),
            WorkItemName = environment.GetEnvironmentVariable("HELIX_WORKITEM_FRIENDLYNAME"),
        };
        bool.TryParse(environment.GetEnvironmentVariable("PERFLAB_HIDDEN"), out var hidden);
        run.Hidden = hidden;
        var configs = environment.GetEnvironmentVariable("PERFLAB_CONFIGS");
        if (!string.IsNullOrEmpty(configs)) // configs should be optional.
        {
            foreach (var kvp in configs.Split(';'))
            {
                var split = kvp.Split('=');
                run.Configurations.Add(split[0], split[1]);
            }
        }

        os = new Os()
        {
            Name = $"{RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}",
            MachineName = Environment.MachineName,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Locale = CultureInfo.CurrentUICulture.ToString()
        };

        build = new Build
        {
            Repo = environment.GetEnvironmentVariable("PERFLAB_REPO"),
            Branch = environment.GetEnvironmentVariable("PERFLAB_BRANCH"),
            Architecture = environment.GetEnvironmentVariable("PERFLAB_BUILDARCH"),
            Locale = environment.GetEnvironmentVariable("PERFLAB_LOCALE"),
            GitHash = environment.GetEnvironmentVariable("PERFLAB_HASH"),
            BuildName = environment.GetEnvironmentVariable("PERFLAB_BUILDNUM"),
            TimeStamp = DateTime.Parse(environment.GetEnvironmentVariable("PERFLAB_BUILDTIMESTAMP")),
        };

        foreach (DictionaryEntry entry in environment.GetEnvironmentVariables())
        {
            if (entry.Key.ToString().Equals("PERFLAB_TARGET_FRAMEWORKS", StringComparison.InvariantCultureIgnoreCase))
            {
                build.AdditionalData["targetFrameworks"] = entry.Value.ToString();
            }
            else if(entry.Key.ToString().EndsWith("version", true, CultureInfo.InvariantCulture))
            {
                // Special case the original two special cases, MAUI_VERSION is only needed because runtime based runs use MAUI_VERSION
                if(entry.Key.ToString().Equals("DOTNET_VERSION", StringComparison.InvariantCultureIgnoreCase)){
                    build.AdditionalData["productVersion"] = entry.Value.ToString();
                } else if(entry.Key.ToString().Equals("MAUI_VERSION", StringComparison.InvariantCultureIgnoreCase)){
                    build.AdditionalData["mauiVersion"] = entry.Value.ToString();
                } else if(!string.IsNullOrWhiteSpace(entry.Value.ToString())){
                    build.AdditionalData[entry.Key.ToString()] = entry.Value.ToString();
                }
            }
            else if(entry.Key.ToString().Equals("DOTNET_ROOT", StringComparison.InvariantCultureIgnoreCase))
            {
                (string installerHash, string sdkVersion) = GetDotNetVersionInfo(entry.Value.ToString());
                if (installerHash is not null)
                    build.AdditionalData["installerHash"] = installerHash;
                if (sdkVersion is not null)
                    build.AdditionalData["sdkVersion"] = sdkVersion;
            }
            else if(entry.Key.ToString().StartsWith("PERFLAB_DATA_", true, CultureInfo.InvariantCulture))
            {
                build.AdditionalData[entry.Key.ToString().Substring("PERFLAB_DATA_".Length)] = entry.Value.ToString();
            }
        }

        (string installerHash, string sdkVersion) GetDotNetVersionInfo(string dotnetRoot)
        {
            if (Path.Combine(dotnetRoot, "sdk") is string sdkRootPath && Directory.Exists(sdkRootPath))
            {
                try
                {
                    string versionFile = Directory
                                            .EnumerateFiles(sdkRootPath, ".version", SearchOption.AllDirectories)
                                            .FirstOrDefault();
                    if (versionFile is not null)
                    {
                        string[] lines = File.ReadAllLines(versionFile);
                        string installerHash = lines.Length > 0 ? lines[0] : null;
                        string sdkVersion = lines.Length > 1 ? lines[1] : null;

                        return (installerHash, sdkVersion);
                    }
                    else
                    {
                        Console.WriteLine ($"Failed to find .version file in {sdkRootPath} - {versionFile}");
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Failed to extract dotnet versions from {sdkRootPath}: {ex.Message}");
                }
            }

            return (null, null);
        }
    }
    public string GetJson()
    {
        if (!InLab)
        {
            return null;
        }
        var jsonobj = new
        {
            build,
            os,
            run,
            tests,
        };
        var settings = new JsonSerializerSettings();
        var resolver = new DefaultContractResolver();
        resolver.NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false };
        settings.ContractResolver = resolver;
        return JsonConvert.SerializeObject(jsonobj, Formatting.Indented, settings);
    }

    public string WriteResultTable()
    {
        var ret = new StringBuilder();
        foreach (var test in tests)
        {
            var defaultCounter = test.Counters.Single(c => c.DefaultCounter);
            var topCounters = test.Counters.Where(c => c.TopCounter && !c.DefaultCounter);
            var restCounters = test.Counters.Where(c => !(c.TopCounter || c.DefaultCounter));
            var counterWidth = Math.Max(test.Counters.Max(c => c.Name.Length) + 1, 15);
            var resultWidth = Math.Max(test.Counters.Max(c => c.Results.Max().ToString("F3").Length + c.MetricName.Length) + 2, 15);
            ret.AppendLine(test.Name);
            ret.AppendLine($"{LeftJustify("Metric", counterWidth)}|{LeftJustify("Average",resultWidth)}|{LeftJustify("Min", resultWidth)}|{LeftJustify("Max",resultWidth)}");
            ret.AppendLine($"{new string('-', counterWidth)}|{new string('-', resultWidth)}|{new string('-', resultWidth)}|{new string('-', resultWidth)}");

            ret.AppendLine(Print(defaultCounter, counterWidth, resultWidth));
            foreach(var counter in topCounters)
            {
                ret.AppendLine(Print(counter, counterWidth, resultWidth));
            }
            foreach (var counter in restCounters)
            {
                ret.AppendLine(Print(counter, counterWidth, resultWidth));
            }
        }
        return ret.ToString();
    }
    private string Print(Counter counter, int counterWidth, int resultWidth)
    {
        var average = $"{counter.Results.Average():F3} {counter.MetricName}";
        var max = $"{counter.Results.Max():F3} {counter.MetricName}";
        var min = $"{counter.Results.Min():F3} {counter.MetricName}";
        return $"{LeftJustify(counter.Name, counterWidth)}|{LeftJustify(average, resultWidth)}|{LeftJustify(min, resultWidth)}|{LeftJustify(max, resultWidth)}";
    }

    private string LeftJustify(string str, int width)
    {
        return string.Format("{0,-" + width + "}", str);
    }

    public bool InLab => environment.GetEnvironmentVariable("PERFLAB_INLAB")?.Equals("1") ?? false;
}
