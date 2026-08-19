// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Reporting.Tests;

public class ReporterTests
{
    // this matches the output from the reporter made in GetReporterWithSpecifiedEnvironment
    private const string ExpectedTestTable =
@"TestName
Metric         |Average        |Min            |Max            
---------------|---------------|---------------|---------------
CounterName    |1.100 ns       |1.100 ns       |1.100 ns       
";
    private const string LongCounterNameTable =
@"TestName
Metric                   |Average        |Min            |Max            
-------------------------|---------------|---------------|---------------
ThisIsALongerCounterName |1.100 ns       |1.100 ns       |1.100 ns       
";
    private const string LongResultTable =
@"TestName
Metric         |Average                  |Min                      |Max                      
---------------|-------------------------|-------------------------|-------------------------
CounterName    |10000000000000000.000 ns |10000000000000000.000 ns |10000000000000000.000 ns 
";

    private const string NoResultsTable =
@"TestName
No results in file.
";

    [Fact]
    public void WriteReportTableWithNoCounters()
    {
        var reporter = new Reporter(new NonPerfLabEnvironmentProviderMock());
        var test = new Test { Name = "TestName" };
        reporter.AddTest(test);
        var table = reporter.WriteResultTable();
        Assert.Equal(NoResultsTable, table);
    }

    [Fact]
    public void WriteReportTableWithEmptyResults()
    {
        var reporter = new Reporter(new NonPerfLabEnvironmentProviderMock());
        var test = new Test
        {
            Name = "TestName",
            Counters = [
                new Counter
                {
                    DefaultCounter = true,
                    TopCounter = true,
                    MetricName = "ns",
                    Name = "CounterName",
                    Results = []
                }
            ]
        };
        reporter.AddTest(test);
        var table = reporter.WriteResultTable();
        Assert.Equal(NoResultsTable, table);
    }

    [Fact]
    public void WriteReportTableWithNullResults()
    {
        var reporter = new Reporter(new NonPerfLabEnvironmentProviderMock());
        var test = new Test
        {
            Name = "TestName",
            Counters = [
                new Counter
                {
                    DefaultCounter = true,
                    TopCounter = true,
                    MetricName = "ns",
                    Name = "CounterName",
                    Results = null
                }
            ]
        };
        reporter.AddTest(test);
        var table = reporter.WriteResultTable();
        Assert.Equal(NoResultsTable, table);
    }

    [Fact]
    public void ReporterWithUnsetEnvironmentProducesNoJson()
    {
        var reporter = new Reporter(new NonPerfLabEnvironmentProviderMock());
        Assert.Null(reporter.GetJson());
    }

    [Fact]
    public void WriteReportTableWithEnvironment()
    {
        var environment = new PerfLabEnvironmentProviderMock();
        var reporter = GetReporterWithSpecifiedEnvironment(environment);
        var table = reporter.WriteResultTable();
        Assert.Equal(ExpectedTestTable, table);
    }

    [Fact]
    public void WriteReportTableWithoutEnvironment()
    {
        PerfLabEnvironmentProviderMock environment = new NonPerfLabEnvironmentProviderMock();
        var reporter = GetReporterWithSpecifiedEnvironment(environment);
        var table = reporter.WriteResultTable();
        Assert.Equal(ExpectedTestTable, table);
    }

    [Fact]
    public void WriteReportWithLongNameTableWithoutEnvironment()
    {
        PerfLabEnvironmentProviderMock environment = new NonPerfLabEnvironmentProviderMock();
        var reporter = GetReporterWithSpecifiedEnvironment(environment, counterName: "ThisIsALongerCounterName");
        var table = reporter.WriteResultTable();
        Assert.Equal(LongCounterNameTable, table);
    }

    [Fact]
    public void WriteReportWithLongResultTableWithoutEnvironment()
    {
        PerfLabEnvironmentProviderMock environment = new NonPerfLabEnvironmentProviderMock();
        var reporter = GetReporterWithSpecifiedEnvironment(environment, result: 10000000000000000);
        var table = reporter.WriteResultTable();
        Assert.Equal(LongResultTable, table);
    }

    [Fact]
    public void JsonCanBeGenerated()
    {
        var environment = new PerfLabEnvironmentProviderMock();
        var reporter = GetReporterWithSpecifiedEnvironment(environment);
        var jsonString = reporter.GetJson();

        var jsonObj = JsonConvert.DeserializeObject<Reporter>(jsonString);

        Assert.Equal(environment.GetEnvironmentVariable("HELIX_CORRELATION_ID"), jsonObj.Run.CorrelationId);
        Assert.Equal(environment.GetEnvironmentVariable("HELIX_WORKITEM_FRIENDLYNAME"), jsonObj.Run.WorkItemName);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_PERFHASH"), jsonObj.Run.PerfRepoHash);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_QUEUE"), jsonObj.Run.Queue);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_REPO"), jsonObj.Build.Repo);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_BRANCH"), jsonObj.Build.Branch);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_BUILDARCH"), jsonObj.Build.Architecture);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_LOCALE"), jsonObj.Build.Locale);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_HASH"), jsonObj.Build.GitHash);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_BUILDNUM"), jsonObj.Build.BuildName);
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_BUILDTIMESTAMP"), jsonObj.Build.TimeStamp.ToString("o"));
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_HIDDEN"), jsonObj.Run.Hidden.ToString().ToLower());
        Assert.Equal(environment.GetEnvironmentVariable("PERFLAB_RUNNAME"), jsonObj.Run.Name);

        Assert.Collection(jsonObj.Run.Configurations,
                          a => { Assert.Equal("KEY1", a.Key); Assert.Equal("VALUE1", a.Value); },
                          a => { Assert.Equal("KEY2", a.Key); Assert.Equal("VALUE2", a.Value); });
        Assert.Equal(RuntimeInformation.OSArchitecture.ToString(), jsonObj.Os.Architecture);
        Assert.Equal("TestName", jsonObj.Tests[0].Name);
        Assert.Equal("UnitTest", jsonObj.Tests[0].Categories[0]);
        var retCounter = jsonObj.Tests[0].Counters[0];
        Assert.Equal("CounterName", retCounter.Name);
        Assert.Equal("ns", retCounter.MetricName);
        Assert.Equal(1.1, retCounter.Results[0]);
    }

    [Fact]
    public void LegacyCounterJsonRemainsCompatible()
    {
        var reporter = GetReporterWithSpecifiedEnvironment(new PerfLabEnvironmentProviderMock());
        var jsonString = reporter.GetJson();
        var jsonCounter = JObject.Parse(jsonString)["tests"][0]["counters"][0];

        Assert.Equal(JTokenType.Boolean, jsonCounter["higherIsBetter"].Type);
        Assert.False(jsonCounter["higherIsBetter"].Value<bool>());
        Assert.Null(jsonCounter["regressionThreshold"]);
        Assert.Null(jsonCounter["direction"]);

        var deserialized = JsonConvert.DeserializeObject<Reporter>(jsonString);
        Assert.False(deserialized.Tests[0].Counters[0].HigherIsBetter);
        Assert.Equal(CounterDirection.LowerIsBetter, deserialized.Tests[0].Counters[0].Direction);
        Assert.Null(deserialized.Tests[0].Counters[0].RegressionThreshold);
    }

    [Fact]
    public void RegressionThresholdIsSerialized()
    {
        var reporter = GetReporterWithSpecifiedEnvironment(new PerfLabEnvironmentProviderMock());
        reporter.Tests[0].Counters[0].RegressionThreshold = 0.02;

        var jsonString = reporter.GetJson();
        var jsonCounter = JObject.Parse(jsonString)["tests"][0]["counters"][0];
        Assert.Equal(0.02, jsonCounter["regressionThreshold"].Value<double>());

        var deserialized = JsonConvert.DeserializeObject<Reporter>(jsonString);
        Assert.Equal(0.02, deserialized.Tests[0].Counters[0].RegressionThreshold);
    }

    [Fact]
    public void UnknownDirectionIsSerializedForNonTopCounter()
    {
        var reporter = GetReporterWithSpecifiedEnvironment(new PerfLabEnvironmentProviderMock());
        reporter.Tests[0].AddCounter(new Counter
        {
            Name = "Storage only",
            Direction = CounterDirection.Unknown,
            MetricName = "value",
            Results = [2.0]
        });

        var jsonString = reporter.GetJson();
        var jsonCounter = JObject.Parse(jsonString)["tests"][0]["counters"][1];
        Assert.Equal(JTokenType.Null, jsonCounter["higherIsBetter"].Type);

        var deserialized = JsonConvert.DeserializeObject<Reporter>(jsonString);
        var counter = deserialized.Tests[0].Counters[1];
        Assert.Equal(CounterDirection.Unknown, counter.Direction);
        Assert.Throws<InvalidOperationException>(() => counter.HigherIsBetter);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidRegressionThresholdIsRejected(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Counter { RegressionThreshold = threshold });
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void UnknownDirectionIsRejectedForDefaultOrTopCounter(bool defaultCounter, bool topCounter)
    {
        var reporter = new Reporter(new PerfLabEnvironmentProviderMock());
        var test = new Test
        {
            Counters = [
                new Counter
                {
                    DefaultCounter = defaultCounter,
                    TopCounter = topCounter,
                    Direction = CounterDirection.Unknown
                }
            ]
        };

        reporter.AddTest(test);
        Assert.Throws<InvalidOperationException>(() => reporter.GetJson());
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void AddCounterRejectsBlankMetricNameForDefaultOrTopCounter(string metricName, bool defaultCounter)
    {
        var test = new Test();

        Assert.Throws<InvalidOperationException>(() => test.AddCounter(new Counter
        {
            DefaultCounter = defaultCounter,
            TopCounter = true,
            MetricName = metricName
        }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SerializationRejectsDirectlyAssignedBlankMetricName(string metricName)
    {
        var reporter = new Reporter(new PerfLabEnvironmentProviderMock());
        reporter.AddTest(new Test
        {
            Counters = [
                new Counter
                {
                    DefaultCounter = true,
                    TopCounter = true,
                    MetricName = metricName
                }
            ]
        });

        Assert.Throws<InvalidOperationException>(() => reporter.GetJson());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DeserializationRejectsBlankMetricName(string metricName)
    {
        var json = new JObject
        {
            ["name"] = "Test",
            ["counters"] = new JArray
            {
                new JObject
                {
                    ["name"] = "Default",
                    ["topCounter"] = true,
                    ["defaultCounter"] = true,
                    ["higherIsBetter"] = false,
                    ["metricName"] = metricName is null ? JValue.CreateNull() : new JValue(metricName)
                }
            }
        }.ToString();

        var exception = Assert.ThrowsAny<Exception>(() => JsonConvert.DeserializeObject<Test>(json));
        Assert.IsType<InvalidOperationException>(exception.GetBaseException());
    }

    [Fact]
    public void ValidMetricNameRoundTrips()
    {
        var reporter = new Reporter(new PerfLabEnvironmentProviderMock());
        var test = new Test();
        test.AddCounter(new Counter
        {
            Name = "Default",
            DefaultCounter = true,
            TopCounter = true,
            MetricName = "ms"
        });
        reporter.AddTest(test);

        var deserialized = JsonConvert.DeserializeObject<Reporter>(reporter.GetJson());
        Assert.Equal("ms", deserialized.Tests[0].Counters[0].MetricName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NonTopCounterAllowsBlankMetricName(string metricName)
    {
        var reporter = GetReporterWithSpecifiedEnvironment(new PerfLabEnvironmentProviderMock());
        reporter.Tests[0].AddCounter(new Counter
        {
            Name = "Storage only",
            MetricName = metricName,
            Results = [2.0]
        });

        var json = reporter.GetJson();
        var deserialized = JsonConvert.DeserializeObject<Reporter>(json);
        Assert.Equal(metricName, deserialized.Tests[0].Counters[1].MetricName);
        Assert.Contains("Storage only", reporter.WriteResultTable());
    }

    [Fact]
    public void SerializationRejectsTestWithoutDefaultCounter()
    {
        var reporter = new Reporter(new PerfLabEnvironmentProviderMock());
        reporter.AddTest(new Test
        {
            Counters = [
                new Counter
                {
                    Name = "Not default"
                }
            ]
        });

        Assert.Throws<InvalidOperationException>(() => reporter.GetJson());
    }

    [Fact]
    public void SerializationRejectsDirectlyAssignedMultipleDefaultCounters()
    {
        var reporter = new Reporter(new PerfLabEnvironmentProviderMock());
        reporter.AddTest(new Test
        {
            Counters = [
                new Counter
                {
                    Name = "First",
                    DefaultCounter = true,
                    TopCounter = true
                },
                new Counter
                {
                    Name = "Second",
                    DefaultCounter = true,
                    TopCounter = true
                }
            ]
        });

        Assert.Throws<InvalidOperationException>(() => reporter.GetJson());
    }

    [Fact]
    public void DeserializationRejectsMultipleDefaultCounters()
    {
        const string json =
@"{
  ""name"": ""Test"",
  ""counters"": [
    {
      ""name"": ""First"",
      ""topCounter"": true,
      ""defaultCounter"": true,
      ""higherIsBetter"": false
    },
    {
      ""name"": ""Second"",
      ""topCounter"": true,
      ""defaultCounter"": true,
      ""higherIsBetter"": false
    }
  ]
}";

        var exception = Assert.ThrowsAny<Exception>(() => JsonConvert.DeserializeObject<Test>(json));
        Assert.IsType<InvalidOperationException>(exception.GetBaseException());
    }

    [Fact]
    public void AddCounterRejectsDefaultCounterThatIsNotTop()
    {
        var test = new Test();

        Assert.Throws<InvalidOperationException>(() => test.AddCounter(new Counter
        {
            DefaultCounter = true
        }));
    }

    [Fact]
    public void SerializationRejectsDirectlyAssignedDuplicateCounterNames()
    {
        var reporter = new Reporter(new PerfLabEnvironmentProviderMock());
        reporter.AddTest(new Test
        {
            Counters = [
                new Counter
                {
                    Name = "Duplicate",
                    DefaultCounter = true,
                    TopCounter = true
                },
                new Counter
                {
                    Name = "Duplicate"
                }
            ]
        });

        Assert.Throws<InvalidOperationException>(() => reporter.GetJson());
    }

    [Fact]
    public void EnforceDefaultCounterConstraint()
    {
        var t = new Test();
        var c = new Counter { DefaultCounter = true, TopCounter = true };
        t.AddCounter(c);
        Assert.Throws<Exception>(() => t.AddCounter(c));
    }

    [Fact]
    public void EnforceUniqueTestNames()
    {
        var r = new Reporter(new PerfLabEnvironmentProviderMock());
        var t = new Test { Name = "Duplicate" };
        r.AddTest(t);
        Assert.Throws<Exception>(() => r.AddTest(t));
    }

    [Fact]
    public void EnforceUniqueCounterName()
    {
        var t = new Test { Name = "Test" };
        var c = new Counter { Name = "Duplicate" };
        t.AddCounter(c);
        Assert.Throws<Exception>(() => { t.AddCounter(c); });
    }

    [Fact]
    public void AddCountersEnumerable()
    {
        var t = new Test();
        var c1 = new Counter { Name = "Counter1", DefaultCounter = true, TopCounter = true };
        var c2 = new Counter { Name = "Counter2" };
        t.AddCounters([c2, c1]);
        Assert.Equal(2, t.Counters.Count);
    }

    private static Reporter GetReporterWithSpecifiedEnvironment(PerfLabEnvironmentProviderMock enviroment, string counterName = null, double result = 1.1)
    {
        var reporter = new Reporter(enviroment);
        var test = new Test
        {
            Name = "TestName",
            Categories = ["UnitTest"],
            Counters = [
                new Counter
                {
                    DefaultCounter = true,
                    TopCounter = true,
                    HigherIsBetter = false,
                    MetricName = "ns",
                    Name = counterName ?? "CounterName",
                    Results = [result]
                }
            ]
        };
        reporter.AddTest(test);
        return reporter;
    }
}
