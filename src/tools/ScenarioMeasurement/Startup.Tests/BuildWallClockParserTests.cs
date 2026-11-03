using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScenarioMeasurement;
using Xunit;

namespace Startup.Tests;

public class BuildWallClockParserTests
{
    [Fact]
    public void PreservesLongBuildDurationsWithoutTraceCollection()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "[12.5, 180000.25]");
            Reporting.Counter counter = Assert.Single(
                new BuildWallClockParser().Parse(path, "dotnet", new List<int>(), ""));
            Assert.Equal("ms", counter.MetricName);
            Assert.True(counter.DefaultCounter);
            Assert.Equal(new[] { 12.5, 180000.25 }, counter.Results);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("[0]")]
    [InlineData("[-1]")]
    public void RejectsMissingOrInvalidMeasurements(string json)
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);
            Assert.Throws<InvalidDataException>(() =>
                new BuildWallClockParser().Parse(path, "dotnet", new List<int>(), "").ToList());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
