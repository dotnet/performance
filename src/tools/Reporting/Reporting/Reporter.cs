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
    private readonly static JsonSerializerSettings _jsonSerializerSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false }
        },
        Culture = _culture
    };

    private readonly static CultureInfo _culture = CultureInfo.InvariantCulture;

    public List<Test> Tests { get; private set; } = [];
    public Run Run { get; private set; }
    public Os Os { get; private set; }
    public Build Build { get; private set; }
    public bool InLab { get; }

    public Reporter(IEnvironment environment = null)
    {
        environment ??= new EnvironmentProvider();
        InLab = environment.IsLabEnvironment();
        if (InLab)
        {
            InitializeFromEnvironment(environment);
        }
    }

    [JsonConstructor]
    public Reporter(Build build, Os os, Run run, List<Test> tests)
    {
        Build = build;
        Os = os;
        Run = run;
        Tests = tests;
        InLab = new EnvironmentProvider().IsLabEnvironment();
    }

    public void AddTest(Test test)
    {
        if (Tests.Any(t => t.Name.Equals(test.Name)))
        {
            throw new Exception($"Duplicate test name, {test.Name}");
        }

        Tests.Add(test);
    }

    public string GetJson()
        => InLab ? JsonConvert.SerializeObject(this, Formatting.Indented, _jsonSerializerSettings) : null;

    public string WriteResultTable()
    {
        var ret = new StringBuilder();
        foreach (var test in Tests)
        {
            var counterWidth = Math.Max(test.Counters.Max(c => c.Name.Length) + 1, 15);
            var resultWidth = Math.Max(test.Counters.Max(c => c.Results.Max().ToString("F3", _culture).Length + c.MetricName.Length) + 2, 15);
            ret.AppendLine(test.Name);
            ret.AppendLine($"{LeftJustify("Metric", counterWidth)}|{LeftJustify("Average", resultWidth)}|{LeftJustify("Min", resultWidth)}|{LeftJustify("Max", resultWidth)}");
            ret.AppendLine($"{new string('-', counterWidth)}|{new string('-', resultWidth)}|{new string('-', resultWidth)}|{new string('-', resultWidth)}");

            var defaultCounter = test.Counters.Single(c => c.DefaultCounter);
            ret.AppendLine(PrintCounter(defaultCounter, counterWidth, resultWidth));
            var topCounters = test.Counters.Where(c => c.TopCounter && !c.DefaultCounter);
            var restCounters = test.Counters.Where(c => !c.TopCounter && !c.DefaultCounter);
            foreach (var counter in topCounters.Concat(restCounters))
            {
                ret.AppendLine(PrintCounter(counter, counterWidth, resultWidth));
            }
        }
        return ret.ToString();
    }

    private void InitializeFromEnvironment(IEnvironment environment)
    {
        Run = ParseRun(environment);
        Os = DetectOs();
        Build = ParseBuildInfo(environment);
    }

    private static Build ParseBuildInfo(IEnvironment environment)
    {
        var build = new Build()
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
            else if (entry.Key.ToString().EndsWith("version", true, CultureInfo.InvariantCulture))
            {
                // Special case the original two special cases, MAUI_VERSION is only needed because runtime based runs use MAUI_VERSION
                if (entry.Key.ToString().Equals("DOTNET_VERSION", StringComparison.InvariantCultureIgnoreCase))
                {
                    build.AdditionalData["productVersion"] = entry.Value.ToString();
                }
                else if (entry.Key.ToString().Equals("MAUI_VERSION", StringComparison.InvariantCultureIgnoreCase))
                {
                    build.AdditionalData["MAUIVERSION"] = entry.Value.ToString();
                }
                else if (!string.IsNullOrWhiteSpace(entry.Value.ToString()))
                {
                    build.AdditionalData[entry.Key.ToString()] = entry.Value.ToString();
                }
            }
            else if (entry.Key.ToString().Equals("DOTNET_ROOT", StringComparison.InvariantCultureIgnoreCase))
            {
                (var installerHash, var sdkVersion) = GetDotNetVersionInfo(entry.Value.ToString());
                if (installerHash is not null)
                    build.AdditionalData["installerHash"] = installerHash;
                if (sdkVersion is not null)
                    build.AdditionalData["sdkVersion"] = sdkVersion;
            }
            else if (entry.Key.ToString().StartsWith("PERFLAB_DATA_", true, CultureInfo.InvariantCulture))
            {
                build.AdditionalData[entry.Key.ToString().Substring("PERFLAB_DATA_".Length)] = entry.Value.ToString();
            }
        }

        return build;
    }

    private static (string installerHash, string sdkVersion) GetDotNetVersionInfo(string dotnetRoot)
    {
        if (Path.Combine(dotnetRoot, "sdk") is string sdkRootPath && Directory.Exists(sdkRootPath))
        {
            try
            {
                var versionFile = Directory
                                        .EnumerateFiles(sdkRootPath, ".version", SearchOption.AllDirectories)
                                        .FirstOrDefault();
                if (versionFile is not null)
                {
                    var lines = File.ReadAllLines(versionFile);
                    var installerHash = lines.Length > 0 ? lines[0] : null;
                    var sdkVersion = lines.Length > 1 ? lines[1] : null;

                    return (installerHash, sdkVersion);
                }
                else
                {
                    Console.WriteLine($"Failed to find .version file in {sdkRootPath} - {versionFile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract dotnet versions from {sdkRootPath}: {ex.Message}");
            }
        }

        return (null, null);
    }

    private static Os DetectOs()
        => new()
        {
            Name = $"{RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}",
            MachineName = System.Environment.MachineName,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Locale = CultureInfo.CurrentUICulture.ToString()
        };

    private static Run ParseRun(IEnvironment environment)
    {
        var run = new Run
        {
            CorrelationId = environment.GetEnvironmentVariable("HELIX_CORRELATION_ID"),
            PerfRepoHash = environment.GetEnvironmentVariable("PERFLAB_PERFHASH"),
            Name = environment.GetEnvironmentVariable("PERFLAB_RUNNAME"),
            Queue = environment.GetEnvironmentVariable("PERFLAB_QUEUE"),
            WorkItemName = environment.GetEnvironmentVariable("HELIX_WORKITEM_FRIENDLYNAME"),
        };

        if (bool.TryParse(environment.GetEnvironmentVariable("PERFLAB_HIDDEN"), out var hidden) && hidden)
        {
            run.Hidden = true;
        }

        run.Configurations = ParseRunConfigs(environment);

        return run;
    }

    private static IDictionary<string, string> ParseRunConfigs(IEnvironment environment)
    {
        var configs = new Dictionary<string, string>();
        var configsString = environment.GetEnvironmentVariable("PERFLAB_CONFIGS");
        if (!string.IsNullOrEmpty(configsString)) // configs should be optional.
        {
            foreach (var kvp in configsString.Split([';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var split = kvp.Split('=');

                if (split.Length != 2)
                {
                    continue;
                }

                var key = split[0].Trim();
                if (key.Length > 0)
                {
                    configs[key] = split[1];
                }
            }
        }

        return configs;
    }

    private static string LeftJustify(string str, int width)
        => string.Format("{0,-" + width + "}", str);

    private static string PrintCounter(Counter counter, int counterWidth, int resultWidth)
    {
        var average = ((FormattableString)$"{counter.Results.Average():F3} {counter.MetricName}").ToString(_culture);
        var max = ((FormattableString)$"{counter.Results.Max():F3} {counter.MetricName}").ToString(_culture);
        var min = ((FormattableString)$"{counter.Results.Min():F3} {counter.MetricName}").ToString(_culture);
        return $"{LeftJustify(counter.Name, counterWidth)}|{LeftJustify(average, resultWidth)}|{LeftJustify(min, resultWidth)}|{LeftJustify(max, resultWidth)}";
    }
}
