using Akade.IndexedSet.Concurrency;
using BenchmarkDotNet.Attributes;
using System.Collections.Immutable;

namespace Akade.IndexedSet.Benchmarks.RealWorld.EventSourcedAggregateCache;

[BenchmarkCategory(Categories.AkadeIndexedSet)]
public class QueryBenchmarks
{
    private readonly ConcurrentIndexedSet<AggregateId, Aggregate> _set = IndexedSetBuilder<Aggregate>.Create(x => x.Id)
                                                                                                     .WithIndex(AggregateIndices.TenantsWithAccess)
                                                                                                     .WithFullTextIndex(AggregateIndices.FullName)
                                                                                                     .BuildConcurrent();

    public QueryBenchmarks()
    {
        EventHandlers.HandleEvents(_set, DataGenerator.GenerateEvents());
    }

    [Benchmark]
    public AggregateDto? GetViaPrimaryId()
    {
        return _set.TryGetSingle(new AggregateId(200), out Aggregate? aggregate)
            ? ToDto(aggregate)
            : null;
    }

    private static AggregateDto ToDto(Aggregate aggregate)
    {
        return new AggregateDto(aggregate.Id, aggregate.Owner, aggregate.SharedWith, aggregate.ExternalId, aggregate.FirstName, aggregate.LastName);
    }

    [Benchmark]
    public AggregateDto[] GetAll()
    {
        return _set.Where(AggregateIndices.TenantsWithAccess, new TenantId(10))
                   .Select(ToDto)
                   .ToArray();
    }

    [Benchmark]
    public AggregateDto[] Search()
    {
        return _set.FuzzyContains(AggregateIndices.FullName, "Carol Ondricka", 2)
                   .Select(ToDto)
                   .ToArray();
    }
}

public record AggregateDto(AggregateId Id, TenantId Owner, ImmutableHashSet<TenantId> SharedWith, ExternalAggregateId ExternalId, string FirstName, string LastName);
