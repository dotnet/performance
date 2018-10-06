using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
            => BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, GetConfig());

        private static IConfig GetConfig()
            => DefaultConfig.Instance
                .With(MemoryDiagnoser.Default)
                .With(new OperatingSystemFilter())
                .With(JsonExporter.Full) // make sure we export to Json (for BenchView integration purpose)
                .With(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .With(TooManyTestCasesValidator.FailOnError);
    }
}