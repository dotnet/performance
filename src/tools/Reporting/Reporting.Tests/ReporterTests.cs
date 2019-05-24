// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Reporting.Tests
{
    public class ReporterTests
    {
        [Fact]
        public void GetReporterWithUnsetEnvironmentReturnsNull()
        {
            var reporter = Reporter.CreateReporter(new NonPerfLabEnvironmentProviderMock());
            Assert.Null(reporter);
        }

        [Fact]
        public void JsonCanBeGenerated()
        {
            PerfLabEnvironmentProviderMock environment = new PerfLabEnvironmentProviderMock();
            var reporter = Reporter.CreateReporter(environment);
            var test = new Test();
            test.Name = "Test Test";
            test.Categories.Add("UnitTest");
            var counter = new Counter();
            counter.DefaultCounter = true;
            counter.HigherIsBetter = false;
            counter.MetricName = "ns";
            counter.Name = "CounterName";
            counter.Results = new[] { 1.1 };
            test.AddCounter(counter);
            reporter.AddTest(test);
            var jsonString = reporter.GetJson();
            var jsonType = new
            {
                Build = default(Build),
                Os = default(Os),
                Run = default(Run),
                Tests = default(Test[])
            };
            var jsonObj = JsonConvert.DeserializeAnonymousType(jsonString, jsonType);

            Assert.Equal(environment.GetEnvironmentVariable("HELIX_CORRELATION_ID"), jsonObj.Run.CorrelationId);
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
            Assert.Equal("Test Test", jsonObj.Tests[0].Name);
            Assert.Equal("UnitTest", jsonObj.Tests[0].Categories[0]);
            var retCounter = jsonObj.Tests[0].Counters[0];
            Assert.Equal("CounterName", retCounter.Name);
            Assert.Equal("ns", retCounter.MetricName);
            Assert.Equal(1.1, retCounter.Results[0]);
        }

        [Fact]
        public void EnforceDefaultCounterConstraint()
        {
            Test t = new Test();
            Counter c = new Counter();
            c.DefaultCounter = true;
            t.AddCounter(c);
            Assert.Throws<Exception>(() => t.AddCounter(c));
        }

        [Fact]
        public void EnforceUniqueTestNames()
        {
            Reporter r = Reporter.CreateReporter(new PerfLabEnvironmentProviderMock());
            Test t = new Test();
            t.Name = "Duplicate";
            r.AddTest(t);
            Assert.Throws<Exception>(() => { r.AddTest(t); });
        }
    }
}