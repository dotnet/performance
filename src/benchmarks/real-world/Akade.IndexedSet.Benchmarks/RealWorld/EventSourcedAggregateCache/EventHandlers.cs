using Akade.IndexedSet.Concurrency;
using System.Collections.Immutable;

namespace Akade.IndexedSet.Benchmarks.RealWorld.EventSourcedAggregateCache;
internal static class EventHandlers
{
    private static readonly Dictionary<Type, Action<ConcurrentIndexedSet<AggregateId, Aggregate>, AggregateEvent>> _eventHandler = new()
        {
            { typeof(AggregateAdded), (state, @event) => HandleEvent(state, (AggregateAdded)@event) },
            { typeof(AggregateShared), (state, @event) => HandleEvent(state, (AggregateShared)@event) }
    };

    private static void HandleEvent(ConcurrentIndexedSet<AggregateId, Aggregate> state, AggregateShared @event)
    {
        if (state.TryGetSingle(@event.Id, out Aggregate? existingAggregate))
        {
            _ = state.Update(existingAggregate, aggregate => aggregate with
            {
                SharedWith = existingAggregate.SharedWith.Add(@event.SharedWith)
            });
        }
    }

    private static void HandleEvent(ConcurrentIndexedSet<AggregateId, Aggregate> state, AggregateAdded @event)
    {
        _ = state.Add(new Aggregate(@event.Id, @event.Owner, ImmutableHashSet<TenantId>.Empty, @event.ExternalId, @event.FirstName, @event.LastName));
    }

    internal static void HandleEvents(ConcurrentIndexedSet<AggregateId, Aggregate> state, IEnumerable<AggregateEvent> events)
    {
        // in the real case, the event dispatching as well as the reading of past events is more complicated (uses the DI container)
        foreach (AggregateEvent @event in events)
        {
            HandleEvent(state, @event);
        }
    }

    internal static void HandleEvent(ConcurrentIndexedSet<AggregateId, Aggregate> state, AggregateEvent @event)
    {
        _eventHandler[@event.GetType()].Invoke(state, @event);
    }
}
