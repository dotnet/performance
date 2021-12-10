using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class StringReaderReadToEndTests : TextReaderReadLineTests
    {
        [GlobalSetup]
        public void GlobalSetup() => _text = GenerateLinesText(LineLengthRange, 16 * 1024);

        [Benchmark]
        public void ReadLine()
        {
            using StringReader reader = new (_text);
            reader.ReadToEnd();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task ReadLineAsync()
        {
            using StringReader reader = new(_text);
            await reader.ReadToEndAsync();
        }
    }
}