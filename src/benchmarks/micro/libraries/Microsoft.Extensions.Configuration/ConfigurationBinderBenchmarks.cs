// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ConfigurationBinderBenchmarks
    {
        [Params(8, 16, 32)]
        public int ConfigurationProvidersCount { get; set; }

        [Params(10, 20, 40)]
        public int KeysCountPerProvider { get; set; }

        private IConfiguration _configuration;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var builder = new ConfigurationBuilder();
            for (int i = 0; i < this.ConfigurationProvidersCount; i++)
            {
                var s = new MySettings
                {
                    IdMapping = Enumerable.Range(0, this.KeysCountPerProvider).ToDictionary(j => $"{i}_{j}", j => $"{j}_{i}")
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
