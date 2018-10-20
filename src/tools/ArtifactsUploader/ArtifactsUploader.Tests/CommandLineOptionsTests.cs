using System.IO;
using System.Linq;
using Moq;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Sdk;

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
            "--workplace=.",
            "--storageUrl=https://portal.azure.com/",
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

        [Theory]
        [InlineData("artifacts", "--artifacts=\"Z:\\not\\existing\\I\\hope\"")]
        [InlineData("workplace", "--workplace=\"Z:\\not\\existing\\I\\hope\"")]
        public void NonExistingDirectoriesAreReportedAsErrors(string argumentName, string invalidValue)
        {
            var loggerMock = LoggerMockHelpers.CreateLoggerMock();
            
            var withUppercaseArgument = CorrectArguments.Select(arg => arg.StartsWith($"--{argumentName}=") ? invalidValue : arg).ToArray();
            
            var result = CommandLineOptions.Parse(withUppercaseArgument, loggerMock.Object);
            
            Assert.False(result.isSuccess);
            LoggerMockHelpers.AssertAtLeastOneErrorWasWrittenToLog(loggerMock);
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TimoutMustBePositiveNumber(int timeout)
        {
            var loggerMock = LoggerMockHelpers.CreateLoggerMock();
            
            var withNonPositiveTimeout = CorrectArguments.Concat(new[] {$"--timeoutMinutes={timeout}"}).ToArray();
            
            var result = CommandLineOptions.Parse(withNonPositiveTimeout, loggerMock.Object);
            
            Assert.False(result.isSuccess);
            
            LoggerMockHelpers.AssertAtLeastOneErrorWasWrittenToLog(loggerMock);
        }
    }
}