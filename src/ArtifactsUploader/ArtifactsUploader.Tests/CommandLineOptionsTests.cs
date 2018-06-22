using System.IO;
using System.Linq;
using Moq;
using Serilog;
using Serilog.Events;
using Xunit;

namespace ArtifactsUploader.Tests
{
    public class CommandLineOptionsTests
    {
        private static readonly string[] CorrectArguments =
        {
            "--project=CoreCLR", 
            "--branch=master", 
            "--job=veryNiceCiJobName",
            "--isPr=false", 
            $"--artifacts={Directory.GetCurrentDirectory()}", 
            "--searchPatterns", "*.log", "*.txt"
        };
        
        [Fact]
        public void CorrectArgumentsAreSuccesfullyParsed()
        {
            var loggerMock = CreateLoggerMock();
            
            var result = CommandLineOptions.Parse(CorrectArguments, loggerMock.Object);
            
            Assert.True(result.isSuccess);
            
            AssertNothingWasWrittenToLog(loggerMock);
        }

        [Fact]
        public void UnknownArgumentsAreNotIgnored()
        {
            var loggerMock = CreateLoggerMock();
            
            var withUnknownArgument = CorrectArguments.Concat(new[] {"--unknown blabla"}).ToArray();
            
            var result = CommandLineOptions.Parse(withUnknownArgument, loggerMock.Object);
            
            Assert.False(result.isSuccess);
            
            AssertAtLeastOneErrorWasWrittenToLog(loggerMock);
        }

        [Fact]
        public void ArgumentsAreCaseInsensitive()
        {
            var loggerMock = CreateLoggerMock();
            
            var withUppercaseArgument = CorrectArguments.Select(arg => arg.Replace("--isPr=false", "--isPr=true")).ToArray();
            
            var result = CommandLineOptions.Parse(withUppercaseArgument, loggerMock.Object);
            
            Assert.True(result.isSuccess);
            Assert.True(result.options.IsPr);
            
            AssertNothingWasWrittenToLog(loggerMock);
        }

        [Fact]
        public void NonExistingArtifactsDirectoryIsReportedAsError()
        {
            var loggerMock = CreateLoggerMock();
            
            var withUppercaseArgument = CorrectArguments.Select(arg => arg.StartsWith("--artifacts=") ? $"--artifacts={@"Z:\not\existing\I\hope"}" : arg).ToArray();
            
            var result = CommandLineOptions.Parse(withUppercaseArgument, loggerMock.Object);
            
            Assert.False(result.isSuccess);
            AssertAtLeastOneErrorWasWrittenToLog(loggerMock);
        }

        private static Mock<ILogger> CreateLoggerMock()
        {
            var loggerMock = new Mock<ILogger>();
            
            loggerMock.Setup(logger => logger.Error(It.IsAny<string>())).Verifiable();
            loggerMock.Setup(logger => logger.Write(It.IsAny<LogEventLevel>(), It.IsAny<string>())).Verifiable();

            return loggerMock;
        } 

        private void AssertNothingWasWrittenToLog(Mock<ILogger> loggerMock) 
            => loggerMock.Verify(log => log.Write(It.IsAny<LogEventLevel>(), It.IsAny<string>()), Times.Never);

        private void AssertAtLeastOneErrorWasWrittenToLog(Mock<ILogger> loggerMock) 
            => loggerMock.Verify(log => log.Error(It.IsAny<string>()), Times.AtLeastOnce);
    }
}