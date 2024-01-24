using Akade.IndexedSet.Concurrency;
using BenchmarkDotNet.Attributes;
using Bogus;

namespace Akade.IndexedSet.Benchmarks;

[BenchmarkCategory(Categories.AkadeIndexedSet)]
public class ConcurrentSetBenchmarks
{
    private readonly List<Person> _persons;
    private readonly IndexedSet<Person> _indexedSet;
    private readonly ConcurrentIndexedSet<Person> _concurrentIndexedSet;

    public ConcurrentSetBenchmarks()
    {
        Randomizer.Seed = new Random(42);
        _persons = Enumerable.Range(0, 1000)
                             .Select(_ => new Person())
                             .ToList();

        _indexedSet = _persons.ToIndexedSet()
                              .WithUniqueIndex(x => x.Phone)
                              .WithFullTextIndex(x => x.FullName)
                              .WithRangeIndex(GetAge)
                              .Build();

        _concurrentIndexedSet = _persons.ToIndexedSet()
                                        .WithUniqueIndex(x => x.Phone)
                                        .WithFullTextIndex(x => x.FullName)
                                        .WithRangeIndex(GetAge)
                                        .BuildConcurrent();
    }

    public static int GetAge(Person p)
    {
        DateTime today = DateTime.Today;
        int age = today.Year - p.DateOfBirth.Year;

        if (p.DateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    [Benchmark]
    public bool UniqueLookup()
    {
        return _indexedSet.TryGetSingle(x => x.Phone, "random", out _);
    }

    [Benchmark]
    public bool ConcurrentUniqueLookup()
    {
        return _concurrentIndexedSet.TryGetSingle(x => x.Phone, "random", out _);
    }

    [Benchmark]
    public int LessThanLookup()
    {
        return _indexedSet.LessThan(GetAge, 12).Count();
    }

    [Benchmark]
    public int ConcurrentLessThanLookup()
    {
        return _concurrentIndexedSet.LessThan(GetAge, 12).Count();
    }

    [Benchmark]
    public int FullTextLookup()
    {
        return _indexedSet.FuzzyContains(x => x.FullName, "Peter", 1).Count();
    }

    [Benchmark]
    public int ConcurrentFullTextLookup()
    {
        return _concurrentIndexedSet.FuzzyContains(x => x.FullName, "Peter", 1).Count();
    }
}
