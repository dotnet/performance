using System.IO;
using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using System.Collections.Immutable;
using System.Reflection;

[assembly: MicroBenchmarks.VSTestConfigSource]

namespace MicroBenchmarks
{
    [AttributeUsage(AttributeTargets.Assembly)]
    class VSTestConfigSourceAttribute : Attribute, IConfigSource
    {
        public VSTestConfigSourceAttribute()
        {
            // We only want to set an assembly-level config when it isn't being set by the entry point
            // We check for this by seeing if the calling assembly is the same as the executing assembly
            Config = Assembly.GetEntryAssembly() == Assembly.GetExecutingAssembly()
                ? ManualConfig.CreateEmpty()
                : RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts")),
                    mandatoryCategories: ImmutableHashSet.Create(Categories.Libraries, Categories.Runtime, Categories.ThirdParty));
        }

        public IConfig Config { get; }
    }
}