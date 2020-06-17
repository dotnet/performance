﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance.Api;
using RealWorld.Utilities;

namespace RealWorld
{
    class MusicStoreBenchmark : WebAppBenchmark
    {
        public MusicStoreBenchmark() : base("MusicStore", "MusicStore.dll") { }

        protected override string RepoUrl => "https://github.com/dotnet-perf-bot/MusicStore";

        protected override string CommitSha1Id => "4de93ebb831404d3db3457142c629b7b0b3eda1b";

        protected override string GetRepoRootDir(string outputDir) => Path.Combine(outputDir, "M");

        protected override string GetSrcDirectory(string outputDir) => Path.Combine(GetRepoRootDir(outputDir), "samples", "MusicStore");
    }

    class AllReadyBenchmark : WebAppBenchmark
    {
        public AllReadyBenchmark() : base("AllReady", "AllReady.dll") { }

        protected override string RepoUrl => "https://github.com/dotnet-perf-bot/allReady";

        protected override string CommitSha1Id => "15e97148c0aa4ef4b0dd02f646665c55e8b820df";

        protected override string GetRepoRootDir(string outputDir) => Path.Combine(outputDir, "A");

        protected override string GetSrcDirectory(string outputDir) => Path.Combine(GetRepoRootDir(outputDir), "AllReadyApp", "Web-App", "AllReady");
    }

    abstract class WebAppBenchmark : Benchmark
    {
        private const string StoreDirName = ".store";
        private readonly Metric StartupMetric = new Metric("Startup", "ms");
        private readonly Metric FirstRequestMetric = new Metric("First Request", "ms");
        private readonly Metric MedianResponseMetric = new Metric("Median Response", "ms");

        public WebAppBenchmark(string name, string executableName) : base(name) => ExePath = executableName;

        protected abstract string RepoUrl { get; }

        protected abstract string CommitSha1Id { get; }

        protected abstract string GetRepoRootDir(string outputDir);

        protected abstract string GetSrcDirectory(string outputDir);

        public override async Task Setup(DotNetInstallation dotNetInstall, string outputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            if (!useExistingSetup)
            {
                using (var setupSection = new IndentedTestOutputHelper("Setup " + Name, output))
                {
                    await CloneWebAppRepo(outputDir, setupSection);
                    await CreateStore(dotNetInstall, outputDir, setupSection);
                    await Publish(dotNetInstall, outputDir, setupSection);
                }
            }

            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            WorkingDirPath = GetWebAppPublishDirectory(dotNetInstall, outputDir, tfm);
            EnvironmentVariables.Add("DOTNET_SHARED_STORE", GetWebAppStoreDir(outputDir));
        }

        async Task CloneWebAppRepo(string outputDir, ITestOutputHelper output)
        {
            // If the repo already exists, we delete it and extract it again.
            string repoRootDir = GetRepoRootDir(outputDir);
            FileTasks.DeleteDirectory(repoRootDir, output);

            await GitTasks.Clone(RepoUrl, repoRootDir, output);
            await GitTasks.Checkout(CommitSha1Id, output, repoRootDir);
        }

        private async Task CreateStore(DotNetInstallation dotNetInstall, string outputDir, ITestOutputHelper output)
        {
            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            string rid = $"win7-{dotNetInstall.Architecture}";
            string storeDirName = ".store";
            await new ProcessRunner("powershell.exe", $"{GetPathToStoreScript()} -InstallDir {storeDirName} -Architecture {dotNetInstall.Architecture} -Runtime {rid} -CreateStoreProjPath {GetPathToCreateStoreProj()}")
                .WithWorkingDirectory(GetSrcDirectory(outputDir))
                .WithEnvironmentVariable("PATH", $"{dotNetInstall.DotNetDir};{Environment.GetEnvironmentVariable("PATH")}")
                .WithEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .WithEnvironmentVariable("SCENARIOS_TARGET_FRAMEWORK_MONIKER", tfm)
                .WithEnvironmentVariable("SCENARIOS_FRAMEWORK_VERSION", dotNetInstall.FrameworkVersion)
                .WithLog(output)
                .Run();
        }

        private async Task<string> Publish(DotNetInstallation dotNetInstall, string outputDir, ITestOutputHelper output)
        {
            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            string publishDir = GetWebAppPublishDirectory(dotNetInstall, outputDir, tfm);
            string manifestPath = Path.Combine(GetWebAppStoreDir(outputDir), dotNetInstall.Architecture, tfm, "artifact.xml");
            if (publishDir != null)
            {
                FileTasks.DeleteDirectory(publishDir, output);
            }
            string dotNetExePath = dotNetInstall.DotNetExe;
            await new ProcessRunner(dotNetExePath, $"publish -c Release -f {tfm} --manifest {manifestPath}")
                .WithWorkingDirectory(GetSrcDirectory(outputDir))
                .WithEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .WithEnvironmentVariable("SCENARIOS_ASPNET_VERSION", "2.0")
                .WithEnvironmentVariable("SCENARIOS_TARGET_FRAMEWORK_MONIKER", tfm)
                .WithEnvironmentVariable("SCENARIOS_FRAMEWORK_VERSION", dotNetInstall.FrameworkVersion)
                .WithEnvironmentVariable("UseSharedCompilation", "false")
                .WithLog(output)
                .Run();

            publishDir = GetWebAppPublishDirectory(dotNetInstall, outputDir, tfm);
            if (publishDir == null)
            {
                throw new DirectoryNotFoundException("Could not find 'publish' directory");
            }
            return publishDir;
        }

        public override Metric[] GetDefaultDisplayMetrics()
        {
            return new Metric[]
            {
                StartupMetric,
                FirstRequestMetric,
                MedianResponseMetric
            };
        }

        protected override IterationResult RecordIterationMetrics(ScenarioExecutionResult scenarioIteration, string stdout, string stderr, ITestOutputHelper output)
        {
            IterationResult result = base.RecordIterationMetrics(scenarioIteration, stdout, stderr, output);
            AddConsoleMetrics(result, stdout, output);
            return result;
        }

        void AddConsoleMetrics(IterationResult result, string stdout, ITestOutputHelper output)
        {
            output.WriteLine("Processing iteration results.");

            double? startupTime = null;
            double? firstRequestTime = null;
            double? steadyStateMedianTime = null;

            using (var reader = new StringReader(stdout))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match match = Regex.Match(line, @"^Server start \(ms\): \s*(\d+)\s*$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        startupTime = Convert.ToDouble(match.Groups[1].Value);
                        continue;
                    }

                    match = Regex.Match(line, @"^1st Request \(ms\): \s*(\d+)\s*$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        firstRequestTime = Convert.ToDouble(match.Groups[1].Value);
                        continue;
                    }

                    //the steady state output chart looks like:
                    //   Requests    Aggregate Time(ms)    Req/s   Req Min(ms)   Req Mean(ms)   Req Median(ms)   Req Max(ms)   SEM(%)
                    // ----------    ------------------    -----   -----------   ------------   --------------   -----------   ------
                    //    2-  100                 5729   252.60          3.01           3.96             3.79          9.81     1.86
                    //  101-  250                 6321   253.76          3.40           3.94             3.84          5.25     0.85
                    //  ... many more rows ...

                    //                              Requests       Agg     req/s        min          mean           median         max          SEM
                    match = Regex.Match(line, @"^\s*\d+-\s*\d+ \s* \d+ \s* \d+\.\d+ \s* \d+\.\d+ \s* (\d+\.\d+) \s* (\d+\.\d+) \s* \d+\.\d+ \s* \d+\.\d+$");
                    if (match.Success && match.Groups.Count == 3)
                    {
                        //many lines will match, but the final values of these variables will be from the last batch which is presumably the
                        //best measurement of steady state performance
                        steadyStateMedianTime = Convert.ToDouble(match.Groups[2].Value, CultureInfo.InvariantCulture);
                        continue;
                    }
                }
            }

            if (!startupTime.HasValue)
                throw new FormatException("Startup time was not found.");
            if (!firstRequestTime.HasValue)
                throw new FormatException("First Request time was not found.");
            if (!steadyStateMedianTime.HasValue)
                throw new FormatException("Steady state median response time not found.");


            result.Measurements.Add(StartupMetric, startupTime.Value);
            result.Measurements.Add(FirstRequestMetric, firstRequestTime.Value);
            result.Measurements.Add(MedianResponseMetric, steadyStateMedianTime.Value);

            output.WriteLine($"Server started in {startupTime}ms");
            output.WriteLine($"Request took {firstRequestTime}ms");
            output.WriteLine($"Median steady state response {steadyStateMedianTime.Value}ms");
        }

        /// <summary>
        /// When serializing the result data to benchview this is called to determine if any of the metrics should be reported differently
        /// than they were collected. Both web apps use this to collect several measurements in each iteration, then present those measurements
        /// to benchview as if each was the Duration metric of a distinct scenario test with its own set of iterations.
        /// </summary>
        public override bool TryGetBenchviewCustomMetricReporting(Metric originalMetric, out Metric newMetric, out string newScenarioModelName)
        {
            if (originalMetric.Equals(StartupMetric))
            {
                newScenarioModelName = "Startup";
            }
            else if (originalMetric.Equals(FirstRequestMetric))
            {
                newScenarioModelName = "First Request";
            }
            else if (originalMetric.Equals(MedianResponseMetric))
            {
                newScenarioModelName = "Median Response";
            }
            else
            {
                return base.TryGetBenchviewCustomMetricReporting(originalMetric, out newMetric, out newScenarioModelName);
            }
            newMetric = Metric.ElapsedTimeMilliseconds;
            return true;
        }

        private string GetPathToStoreScript() => Path.Combine(Path.GetDirectoryName(typeof(WebAppBenchmark).Assembly.Location), "Store", "AspNet-GenerateStore.ps1"); // the script is a content copied to output dir

        private string GetPathToCreateStoreProj() => Path.Combine(Path.GetDirectoryName(typeof(WebAppBenchmark).Assembly.Location), "Store", "CreateStore", "CreateStore._proj_"); // the proj is a content copied to output dir

        private string GetWebAppStoreDir(string outputDir) => Path.Combine(GetSrcDirectory(outputDir), StoreDirName);

        private string GetWebAppPublishDirectory(DotNetInstallation dotNetInstall, string outputDir, string tfm)
        {
            string dir = Path.Combine(GetSrcDirectory(outputDir), "bin", dotNetInstall.Architecture, "Release", tfm, "publish");
            if (Directory.Exists(dir))
            {
                return dir;
            }

            dir = Path.Combine(GetSrcDirectory(outputDir), "bin", "Release", tfm, "publish");
            if (Directory.Exists(dir))
            {
                return dir;
            }

            return null;
        }
    }
}
