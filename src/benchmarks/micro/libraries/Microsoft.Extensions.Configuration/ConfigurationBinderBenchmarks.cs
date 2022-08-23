using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Configuration
{
    [BenchmarkCategory(Categories.Libraries)]
    public class ConfigurationBinderBenchmarks
    {
        public static IEnumerable<int> GetDuplicateCount() => new int[] { 2, 4, 6};

        public static IEnumerable<int> GetMyObjectCount() => new int[] { 3, 6, 9};

        [ParamsSource(nameof(GetMyObjectCount))]
        public int MyObjectCount { get; set; }

        [ParamsSource(nameof(GetDuplicateCount))]
        public int DuplicateCount { get; set; }

        private IConfiguration _configuration;

        private readonly List<MemoryStream> _streams = new List<MemoryStream>();

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
                var jsonString = JsonConvert.SerializeObject(s);
                for (var j = 0; j < DuplicateCount; j++)
                {
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
                    this._streams.Add(stream);
                    builder.AddJsonStream(stream);
                }
            }

            _configuration = builder.Build();            
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (var s in this._streams)
            {
                s.Dispose();
            }
        }

        [Benchmark]
        public void Get()
        {
            _configuration.Get<MySettings>();
        }

        public class MySettings
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
            public IDictionary<string, MySettings> SubSettings { get; set; }
        }
    }
}
