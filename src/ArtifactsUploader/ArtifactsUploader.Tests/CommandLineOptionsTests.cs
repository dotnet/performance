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
            var loggerMock = LoggerMockHelpers.CreateLoggerMock();
            
            var result = CommandLineOptions.Parse(CorrectArguments, loggerMock.Object);
            
            Assert.True(result.isSuccess);
            
            LoggerMockHelpers.AssertNothingWasWrittenToLog(loggerMock);
        }

        [Fact]
        public void UnknownArgumentsAreNotIgnored()
        {
            var loggerMock = LoggerMockHelpers.CreateLoggerMock();
            
            var withUnknownArgument = CorrectArguments.Concat(new[] {"--unknown blabla"}).ToArray();
            
            var result = CommandLineOptions.Parse(withUnknownArgument, loggerMock.Object);
            
            Assert.False(result.isSuccess);
            
            LoggerMockHelpers.AssertAtLeastOneErrorWasWrittenToLog(loggerMock);
        }

        [Fact]
        public void ArgumentsAreCaseInsensitive()
        {
            var loggerMock = LoggerMockHelpers.CreateLoggerMock();
            
            var withUppercaseArgument = CorrectArguments.Select(arg => arg.Replace("--isPr=false", "--isPr=true")).ToArray();
            
            var result = CommandLineOptions.Parse(withUppercaseArgument, loggerMock.Object);
            
            Assert.True(result.isSuccess);
            Assert.True(result.options.IsPr);
            
            LoggerMockHelpers.AssertNothingWasWrittenToLog(loggerMock);
        }

        [Fact]
        public void NonExistingArtifactsDirectoryIsReportedAsError()
        {
            var loggerMock = LoggerMockHelpers.CreateLoggerMock();
            
            var withUppercaseArgument = CorrectArguments.Select(arg => arg.StartsWith("--artifacts=") ? $"--artifacts={@"Z:\not\existing\I\hope"}" : arg).ToArray();
            
            var result = CommandLineOptions.Parse(withUppercaseArgument, loggerMock.Object);
            
            Assert.False(result.isSuccess);
            LoggerMockHelpers.AssertAtLeastOneErrorWasWrittenToLog(loggerMock);
        }
    }
}