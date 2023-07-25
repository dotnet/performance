using System.Diagnostics.Tracing;

[EventSource(Guid = "9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")]
class PerfLabGenericEventSource : EventSource
{
    private const int MagicConstant = 6666;

    public static PerfLabGenericEventSource Log { get; } = new PerfLabGenericEventSource();

    public void Startup() => WriteEvent(MagicConstant + 1);

    public void OnMain() => WriteEvent(MagicConstant + 2);
}