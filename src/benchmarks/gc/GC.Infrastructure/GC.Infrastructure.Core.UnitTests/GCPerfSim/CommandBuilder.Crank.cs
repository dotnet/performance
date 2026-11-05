using GC.Infrastructure.Core.CommandBuilders;
using GC.Infrastructure.Core.Configurations;
using AspNet = GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using PerfSim = GC.Infrastructure.Core.Configurations.GCPerfSim;

namespace GC.Infrastructure.Core.UnitTests.GCPerfSim
{
    [TestClass]
    public sealed class ConfigurationBuilder_Crank
    {
        [DataTestMethod]
        [DataRow(OS.Windows)]
        [DataRow(OS.Linux)]
        public void ServerTraceUsesNet10(OS os)
        {
            var configuration = new PerfSim.GCPerfSimConfiguration
            {
                Name = "test",
                TraceConfigurations = new TraceConfigurations { Type = "gc" },
                Runs = new(),
                coreruns = new(),
                gcperfsim_configurations = new() { gcperfsim_path = "GCPerfSim.dll" },
                Output = new()
                {
                    Path = "traces", Columns = new(), AllColumns = new(), Formats = new()
                }
            };
            var run = new PerfSim.Run { environment_variables = new(), override_parameters = new() };
            var coreRun = new PerfSim.CoreRunInfo
            {
                Path = "corerun.exe", is_baseline = false, environment_variables = new()
            };
            var (process, command) = GCPerfSimCommandBuilder.BuildForServer(
                configuration, new("test", run), 0, new("runtime", coreRun), "local", os);

            Assert.AreEqual("crank", process);
            StringAssert.Contains(command, "--application.framework net10.0");
            StringAssert.Contains(command, "--scenario gcperfsim");
            StringAssert.Contains(command, os == OS.Windows ? "--application.collect true" : "--application.dotnetTrace true");
        }

        [DataTestMethod]
        [DataRow(null, "net10.0")]
        [DataRow("net11.0", "net11.0")]
        public void AspNetDefaultPreservesRunOverride(string? runFramework, string expected)
        {
            var configuration = new AspNet.ASPNetBenchmarksConfiguration
            {
                Name = "test",
                TraceConfigurations = new() { Type = "none" },
                Environment = new(),
                benchmark_settings = new(),
                Output = new()
                {
                    Path = "traces", Columns = new(), AllColumns = new(), Formats = new()
                }
            };
            var run = new AspNet.Run
            {
                framework_version = runFramework, corerun = "corerun.exe", environment_variables = new()
            };

            var (process, command) = ASPNetBenchmarksCommandBuilder.Build(
                configuration, new("test", run), new("benchmark", "--scenario benchmark"), OS.Windows);

            Assert.AreEqual("net10.0", configuration.Environment.framework_version);
            Assert.AreEqual("crank", process);
            StringAssert.Contains(command, $"--application.framework {expected}");
        }
    }
}
