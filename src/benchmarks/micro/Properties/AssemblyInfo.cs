using System.IO;
using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using System.Collections.Immutable;

[assembly: MicroBenchmarks.RecommendedConfigSource]

namespace MicroBenchmarks
{
    [AttributeUsage(AttributeTargets.Assembly)]
    class RecommendedConfigSourceAttribute : Attribute, IConfigSource
    {
        public RecommendedConfigSourceAttribute()
        {
            Config = RecommendedConfig.Create(
                artifactsPath: new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts")),
                mandatoryCategories: ImmutableHashSet.Create(Categories.Libraries, Categories.Runtime, Categories.ThirdParty));
        }

        public IConfig Config { get; }
    }
}