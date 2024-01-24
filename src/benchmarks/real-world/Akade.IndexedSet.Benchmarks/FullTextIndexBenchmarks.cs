using BenchmarkDotNet.Attributes;
using Bogus;

namespace Akade.IndexedSet.Benchmarks;

[BenchmarkCategory(Categories.AkadeIndexedSet)]
public class FullTextIndexBenchmarks
{
    public record class Document(string Content);

    private readonly IndexedSet<Document> _indexedSet;
    private readonly List<Document> _document;

    public FullTextIndexBenchmarks()
    {
        Randomizer.Seed = new Random(42);
        _document = new Faker<Document>().CustomInstantiator(f => new Document(f.Rant.Review()))
                                         .Generate(1000);

        _indexedSet = _document.ToIndexedSet()
                               .WithFullTextIndex(x => x.Content)
                               .Build();

    }

    [Benchmark]
    public Document[] Contains_IndexedSet()
    {
        return _indexedSet.Contains(x => x.Content, "cheeseburger").ToArray();
    }

    [Benchmark]
    public Document[] FuzzyContains_IndexedSet()
    {
        return _indexedSet.FuzzyContains(x => x.Content, "cheeseburger", 2).ToArray();
    }
}
