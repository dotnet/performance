using System.Diagnostics.Tracing;

[EventSource(Guid = "9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")]
class PerfLabGenericEventSource : EventSource
{
    public static PerfLabGenericEventSource Log { get; } = new PerfLabGenericEventSource();

    public void Startup() => WriteEvent(1);

    public void OnMain() => WriteEvent(2);
}