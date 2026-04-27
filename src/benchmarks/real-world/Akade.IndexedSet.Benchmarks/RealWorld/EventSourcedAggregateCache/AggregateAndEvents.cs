using System.Collections.Immutable;

namespace Akade.IndexedSet.Benchmarks.RealWorld.EventSourcedAggregateCache;

public record class Aggregate(AggregateId Id, TenantId Owner, ImmutableHashSet<TenantId> SharedWith, ExternalAggregateId ExternalId, string FirstName, string LastName);
public record struct AggregateId(long Id);
public record struct TenantId(long Id);
public record struct ExternalAggregateId(long PartOne, long PartTwo);

public record class AggregateAdded(AggregateId Id, TenantId Owner, ExternalAggregateId ExternalId, string FirstName, string LastName) : AggregateEvent;
public abstract record class AggregateEvent();
public record class AggregateShared(AggregateId Id, TenantId SharedWith) : AggregateEvent;

internal static class AggregateIndices
{
    internal static IEnumerable<TenantId> TenantsWithAccess(Aggregate aggregate)
    {
        return aggregate.SharedWith.Prepend(aggregate.Owner);
    }

    internal static string FullName(Aggregate aggregate)
    {
        return $"{aggregate.FirstName} {aggregate.LastName}";
    }
}