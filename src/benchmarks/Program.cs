using BenchmarkDotNet.Running;
using Benchmarks.Serializers;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var (success, config) = ConsoleArguments.Parse(args);
            
            if (!success)
                return;
            
            BenchmarkSwitcher
                .FromAssemblyAndTypes(typeof(Program).Assembly, SerializerBenchmarks.GetTypes())
                .Run(config: config);
        }
    }
}
