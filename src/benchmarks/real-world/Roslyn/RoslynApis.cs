
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;

namespace CompilerBenchmarks
{
    /// <summary>
    /// A collection of benchmarks for Roslyn APIs
    /// </summary>
    [BenchmarkCategory("Roslyn")]
    public class RoslynApis
    {
        [Benchmark]
        public object BuildAnalyzerConfigs()
        {
            var cmdLineArgs = Helpers.GetReproCommandLineArgs();
            var analyzerConfigs = cmdLineArgs.AnalyzerConfigPaths
               .SelectAsArray(p => AnalyzerConfig.Parse(File.ReadAllText(p), p));
            var set = AnalyzerConfigSet.Create(analyzerConfigs);

            return cmdLineArgs.SourceFiles
                .SelectAsArray(s => set.GetOptionsForSourcePath(s.Path));
        }
    }
}