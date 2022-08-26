// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ConfigurationBinderBenchmarks
    {
        [Params(3, 6, 9)]
        public int MyObjectCount { get; set; }

        [Params(2, 4, 6)]
        public int DuplicateCount { get; set; }

        private IConfiguration _configuration;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var builder = new ConfigurationBuilder();
            for (int i = 0; i < this.MyObjectCount; i++)
            {
                var s = new MySettings
                {
                    Foo = "root",
                    Bar = int.MaxValue,
                    SubSettings = new Dictionary<string, MySettings>
                    {
                        ["level 1 key " + i] = new MySettings
                        {
                            Foo = "foo" + i,
                            Bar = i,
                            SubSettings = new Dictionary<string, MySettings>
                            {
                                ["level 2 key " + i] = new MySettings
                                {
                                    Foo = "just a simple 2 level tree settings " + i,
                                    Bar = 2 * i,
                                }
                            }
                        }
                    }
                };
                var jsonString = JsonSerializer.Serialize(s);
                for (var j = 0; j < DuplicateCount; j++)
                {
                    builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(jsonString)));
                }
            }

            _configuration = builder.Build();            
        }

        [Benchmark]
        public MySettings Get() =>_configuration.Get<MySettings>();

        public class MySettings
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
            public IDictionary<string, MySettings> SubSettings { get; set; }
        }
    }
}
