using Bogus;

namespace Akade.IndexedSet.Benchmarks.RealWorld.EventSourcedAggregateCache;

internal static class DataGenerator
{
    internal static IEnumerable<AggregateEvent> GenerateEvents(int aggregateCount = 650000)
    {
        const int exclusiveTenantIdBound = 99;
        const int seed = 42;
        const double sharedWithOneProbability = 0.02;
        const double sharedWithTwoProbability = 0.005;
        Random random = new(seed);
        Randomizer.Seed = random;
        Bogus.DataSets.Name name = new();
        for (int i = 1; i <= aggregateCount; i++)
        {
            
            yield return new AggregateAdded(new(i), new(random.Next(1, exclusiveTenantIdBound)), new(i << 1, i << 2), name.FirstName(), name.LastName());

            switch (random.NextDouble())
            {
                case < sharedWithOneProbability:
                    yield return new AggregateShared(new AggregateId(i), new(random.Next(1, exclusiveTenantIdBound)));
                    break;
                case < (sharedWithOneProbability + sharedWithTwoProbability):
                    yield return new AggregateShared(new AggregateId(i), new(random.Next(1, exclusiveTenantIdBound)));
                    yield return new AggregateShared(new AggregateId(i), new(random.Next(1, exclusiveTenantIdBound)));
                    break;
            }
        }
    }
}