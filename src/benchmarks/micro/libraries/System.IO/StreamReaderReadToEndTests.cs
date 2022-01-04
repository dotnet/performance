using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests;

[BenchmarkCategory(Categories.Libraries)]
public class StreamReaderReadToEndTests : TextReaderReadLineTests
{
    private StreamReader _reader;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _text = GenerateLinesText(LineLengthRange, 16 * 1024);
        _reader = new (new MemoryStream(Encoding.UTF8.GetBytes(_text)));
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _reader?.Dispose();
    }

    [Benchmark]
    public void ReadToEnd()
    {
        _reader.BaseStream.Position = 0;
        _reader.DiscardBufferedData();
        _reader.ReadToEnd();
    }

    [Benchmark]
    [BenchmarkCategory(Categories.NoWASM)]
    public async Task ReadToEndAsync()
    {
        _reader.BaseStream.Position = 0;
        _reader.DiscardBufferedData();
        await _reader.ReadToEndAsync();
    }
}