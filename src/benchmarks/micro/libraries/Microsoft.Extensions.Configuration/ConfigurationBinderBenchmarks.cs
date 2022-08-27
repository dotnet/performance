// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ConfigurationBinderBenchmarks
    {
        [Params(32, 64, 128)]
        public int MySettingsCount { get; set; }

        private IConfiguration _configuration;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var builder = new ConfigurationBuilder();
            for (int i = 0; i < this.MySettingsCount; i++)
            {
                var s = new MySettings
                {
                    IdMapping = new Dictionary<string, string> { [i.ToString()] = i.ToString() }
                };
                builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(s))));
            }

            _configuration = builder.Build();            
        }

        [Benchmark]
        public MySettings Get() => _configuration.Get<MySettings>();

        public class MySettings
        {
            public Dictionary<string, string> IdMapping { get; set; }
        }
    }
}
