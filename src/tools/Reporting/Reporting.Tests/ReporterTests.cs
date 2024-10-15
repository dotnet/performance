// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
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
        var reporter = GetReporterWithSpecifiedEnvironment(environment, counterName:"ThisIsALongerCounterName");
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
    public void EnforceDefaultCounterConstraint()
    {
        var t = new Test();
        var c = new Counter { DefaultCounter = true };
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
        var c1 = new Counter { Name = "Counter1", DefaultCounter = true };
        var c2 = new Counter { Name = "Counter2" };
        t.AddCounters([c1, c2]);
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
