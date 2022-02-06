// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_XmlConvert
    {
	    private DateTime _testDateTime;
	    private TimeSpan _testTimeSpan;

        [GlobalSetup]
        public void Setup()
        {
	        _testDateTime = DateTime.UtcNow;
	        _testTimeSpan = new TimeSpan(1, 2, 3, 4, 56);
        }

        [Benchmark]
        public string DateTime_ToString() => XmlConvert.ToString(_testDateTime, XmlDateTimeSerializationMode.Utc);

        [Benchmark]
        public string DateTime_ToString_Local() => XmlConvert.ToString(_testDateTime, XmlDateTimeSerializationMode.Local);

        [Benchmark]
        public string DateTime_ToString_Unspecified() => XmlConvert.ToString(_testDateTime, XmlDateTimeSerializationMode.Unspecified);

        [Benchmark]
        public string DateTime_ToString_RoundtripKind() => XmlConvert.ToString(_testDateTime, XmlDateTimeSerializationMode.RoundtripKind);

        [Benchmark]
        public string TimeSpan_ToString() => XmlConvert.ToString(_testTimeSpan);
    }
}
