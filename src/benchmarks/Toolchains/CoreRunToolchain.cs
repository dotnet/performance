using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace Benchmarks.Toolchains
{
    public class CoreRunToolchain : Toolchain
    {
        /// <summary>
        /// creates a CoreRunToolchain which is using provided CoreRun to execute .NET Core apps
        /// </summary>
        /// <param name="coreRunPath">the path to CoreRun</param>
        /// <param name="targetFrameworkMoniker">TFM, netcoreapp2.1 is the default</param>
        /// <param name="customDotNetCliPath">path to dotnet cli, if not provided the one from PATH will be used</param>
        /// <param name="displayName">display name, CoreRun is the default value</param>
        public CoreRunToolchain(string coreRunPath, 
            string targetFrameworkMoniker = "netcoreapp2.1", string customDotNetCliPath = null,
            string displayName = "CoreRun") 
            : base(
                displayName, 
                new CoreRunGenerator(coreRunPath, targetFrameworkMoniker, platform => platform.ToConfig()),
                new CoreRunPublisher(coreRunPath, customDotNetCliPath),
                new DotNetCliExecutor(customDotNetCliPath: coreRunPath)) // instead of executing "dotnet $pathToDll" we do "CoreRun $pathToDll" 
        {
            CustomDotNetCliPath = customDotNetCliPath;
            CoreRunPath = coreRunPath;
        }

        private string CoreRunPath { get; }

        private string CustomDotNetCliPath { get; }

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
                return false;

            if (string.IsNullOrEmpty(CoreRunPath) || !File.Exists(CoreRunPath))
            {
                logger.WriteLineError($"Povided CoreRun path does not exist, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (string.IsNullOrEmpty(CustomDotNetCliPath) && !HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli toolchain to be installed, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            if (!string.IsNullOrEmpty(CustomDotNetCliPath) && !File.Exists(CustomDotNetCliPath))
            {
                logger.WriteLineError($"Povided custom dotnet cli path does not exist, benchmark '{benchmark.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }
    }
}