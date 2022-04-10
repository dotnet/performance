using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroBenchmarks.libraries.Microsoft.Extensions.Configuration.Binder
{
    [BenchmarkCategory(Categories.Libraries)]
    [MemoryDiagnoser]
    public class ConfigurationBinderBenchmarks
    {
        [Params(0.5, 1)]
        public double ScalarFillFactor;

        [Params(1, 20)]
        public int CollectionSize;

        private IConfiguration _configuration;

        [GlobalSetup]
        public void SetUp()
        {
            var data = new Dictionary<string, string>();

            for (var i = 0; i < 12 * ScalarFillFactor; i++)
            {
                data.Add($"B{i + 1}", (i % 2 == 0).ToString());
            }
            for (var i = 0; i < 6 * ScalarFillFactor; i++)
            {
                data.Add($"I{i + 1}", i.ToString());
            }
            AddSubOptions("O1:");
            for (var i = 0; i < CollectionSize; i++)
            {
                AddSubOptions($"L1:{i}:");
                AddSubOptions($"D1:{i}:");
            }

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(data);
            _configuration = configurationBuilder.Build();

            void AddSubOptions(string prefix)
            {
                data.Add(prefix + "B1", "true");
                data.Add(prefix + "I1", "1000");
                data.Add(prefix + "E1", "IgnoreCase");
                data.Add(prefix + "S1", "text");
                for (var i = 0; i < CollectionSize; i++)
                {
                    data.Add(prefix + "A1:" + i, i.ToString());
                }
            }
        }

        [Benchmark]
        public object GetOptions() => _configuration.Get<BenchmarkOptions>();

        private class BenchmarkOptions
        {
            public bool B1 { get; set; }
            public bool B2 { get; set; }
            public bool B3 { get; set; }
            public bool B4 { get; set; }
            public bool B5 { get; set; }
            public bool B6 { get; set; }
            public bool B7 { get; set; }
            public bool B8 { get; set; }
            public bool B9 { get; set; }
            public bool B10 { get; set; }
            public bool B11 { get; set; }
            public bool B12 { get; set; }

            public int I1 { get; set; }
            public int? I2 { get; set; }
            public int? I3 { get; set; }
            public int I4 { get; set; }
            public int I5 { get; set; }
            public int I6 { get; set; }

            public BenchmarkSubOptions O1 { get; } = new BenchmarkSubOptions();
            public IDictionary<string, BenchmarkSubOptions> D1 { get; } = new Dictionary<string, BenchmarkSubOptions>();
            public IList<BenchmarkSubOptions> L1 { get; } = new List<BenchmarkSubOptions>();
        }

        private class BenchmarkSubOptions
        {
            public bool? B1 { get; set; }
            public int? I1 { get; set; }
            public System.Text.RegularExpressions.RegexOptions? E1 { get; set; }
            public string S1 { get; set; }
            public string[] A1 { get; set; }
        }
    }
}
