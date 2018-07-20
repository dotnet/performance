using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Memory
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class ReadOnlySpan
    {
        private readonly string _sampeString = "this is a very nice sample string";

        [Benchmark]
        public ReadOnlySpan<char> StringAsSpan() => _sampeString.AsSpan();
    }
}