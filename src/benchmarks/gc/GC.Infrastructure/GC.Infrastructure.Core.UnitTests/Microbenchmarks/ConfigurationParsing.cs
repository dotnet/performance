using FluentAssertions;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;

namespace GC.Infrastructure.Core.UnitTests
{
    public class ConfigurationParsing
    {
        private void TestForValidConfiguration(MicrobenchmarkConfiguration configuration)
        {
            // Null check.
            configuration.Should().NotBeNull();

            // Runs.
            configuration.Runs.Should().NotBeNull();
            configuration.Runs.Count.Should().NotBe(0);

            // Microbenchmarks.
            configuration.MicrobenchmarkConfigurations.Should().NotBeNull();
            configuration.MicrobenchmarkConfigurations.Filter.Should().NotBeNull();
            configuration.MicrobenchmarkConfigurations.DotnetInstaller.Should().NotBeNull();

            // Output.
            configuration.Output.Should().NotBeNull();

            // Trace Configurations.
            configuration.TraceConfigurations.Should().NotBeNull();
            configuration.TraceConfigurations.Type.Should().NotBeNull();
        }

        [TestMethod]
        public void Parse_NoMicrobenchmarkConfigurationsSpecified_InvalidAndShouldThrowArgumentNullException()
        {
            string path = Path.Combine(GC.Infrastructure.Core.UnitTests.GCPerfSim.Common.CONFIGURATION_PATH, "NoMicrobenchmarkConfigurationsSpecified.yaml");
            Func<MicrobenchmarkConfiguration> configurationFunc = () => MicrobenchmarkConfigurationParser.Parse(path);
            configurationFunc.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Parse_NoMicrobenchmarkFilterOrFilterFileSpecified_InvalidAndShouldThrowArgumentNullException()
        {
            string path = Path.Combine(GC.Infrastructure.Core.UnitTests.GCPerfSim.Common.CONFIGURATION_PATH, "NoMicrobenchmarkFilterSpecified.yaml");
            Func<MicrobenchmarkConfiguration> configurationFunc = () => MicrobenchmarkConfigurationParser.Parse(path);
            configurationFunc.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Parse_NoMicrobenchmarkFrameworkVersionSpecified_InvalidAndShouldThrowArgumentNullException()
        {
            string path = Path.Combine(GC.Infrastructure.Core.UnitTests.GCPerfSim.Common.CONFIGURATION_PATH, "NoMicrobenchmarkFrameworkVersionSpecified.yaml");
            Func<MicrobenchmarkConfiguration> configurationFunc = () => MicrobenchmarkConfigurationParser.Parse(path);
            configurationFunc.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Parse_NoOutputSpecified_InvalidAndShouldThrowArgumentNullException()
        {
            string path = Path.Combine(GC.Infrastructure.Core.UnitTests.GCPerfSim.Common.CONFIGURATION_PATH, "NoOutputSpecified.yaml");
            Func<MicrobenchmarkConfiguration> configurationFunc = () => MicrobenchmarkConfigurationParser.Parse(path);
            configurationFunc.Should().Throw<ArgumentNullException>();
        }


        [TestMethod]
        public void Parse_NoRunsSpecified_InvalidAndShouldThrowArgumentNullException()
        {
            string path = Path.Combine(GC.Infrastructure.Core.UnitTests.GCPerfSim.Common.CONFIGURATION_PATH, "NoRunsSpecified.yaml");
            Func<MicrobenchmarkConfiguration> configurationFunc = () => MicrobenchmarkConfigurationParser.Parse(path);
            configurationFunc.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Parse_NoTraceConfigurationTypeSpecified_InvalidAndShouldThrowArgumentNullException()
        {
            string path = Path.Combine(GC.Infrastructure.Core.UnitTests.GCPerfSim.Common.CONFIGURATION_PATH, "NoTraceConfigurationTypeSpecified.yaml");
            Func<MicrobenchmarkConfiguration> configurationFunc = () => MicrobenchmarkConfigurationParser.Parse(path);
            configurationFunc.Should().Throw<ArgumentNullException>();
        }
    }
}