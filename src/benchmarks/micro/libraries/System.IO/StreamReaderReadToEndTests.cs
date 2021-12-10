using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests;

[BenchmarkCategory(Categories.Libraries)]
public class StreamReaderReadToEndTests : TextReaderReadLineTests
{
    private byte[] _bytes;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _text = GenerateLinesText(LineLengthRange, 16 * 1024);
        _bytes = Encoding.UTF8.GetBytes(_text);
    }

    [Benchmark]
    public void ReadLine()
    {
        using StreamReader reader = new (new MemoryStream(_bytes));
        reader.ReadToEnd();
    }

    [Benchmark]
    [BenchmarkCategory(Categories.NoWASM)]
    public async Task ReadLineAsync()
    {
        using StreamReader reader = new(new MemoryStream(_bytes));
        await reader.ReadToEndAsync();
    }
}