using BenchmarkDotNet.Attributes;
using Bogus;

namespace Akade.IndexedSet.Benchmarks;

[BenchmarkCategory(Categories.AkadeIndexedSet)]
public class PrefixIndexBenchmarks
{
    private readonly List<Person> _persons;
    private readonly IndexedSet<Person> _indexedSet;

    public PrefixIndexBenchmarks()
    {
        Randomizer.Seed = new Random(42);
        _persons = Enumerable.Range(0, 1000)
                             .Select(_ => new Person())
                             .ToList();

        _indexedSet = _persons.ToIndexedSet()
                              .WithPrefixIndex(x => x.FullName)
                              .Build();
    }

    [Benchmark]
    public Person[] StartsWith_IndexedSet()
    {
        return _indexedSet.StartsWith(x => x.FullName, "Tiffany").ToArray();
    }

    [Benchmark]
    public Person[] FuzzyStartsWith_IndexedSet()
    {
        return _indexedSet.FuzzyStartsWith(x => x.FullName, "Tiffany", 2).ToArray();
    }
}
