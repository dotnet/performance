using Akade.IndexedSet.Benchmarks.RealWorld.EventSourcedAggregateCache;
using Akade.IndexedSet.Concurrency;
using BenchmarkDotNet.Attributes;

namespace Akade.IndexedSet.Benchmarks.RealWorld;

[BenchmarkCategory(Categories.AkadeIndexedSet)]
public class RehydrationBenchmark
{
    private readonly ConcurrentIndexedSet<AggregateId, Aggregate> _set = IndexedSetBuilder<Aggregate>.Create(x => x.Id)
                                                                                                     .WithIndex(x => x.Owner)
                                                                                                     .WithIndex(x => x.SharedWith.Any())
                                                                                                     .WithIndex(AggregateIndices.TenantsWithAccess)
                                                                                                     .WithFullTextIndex(AggregateIndices.FullName)
                                                                                                     .BuildConcurrent();

    [Benchmark]
    public void Rehydration()
    {
        _set.Clear();
        EventHandlers.HandleEvents(_set, DataGenerator.GenerateEvents(30000));
    }
    
}
