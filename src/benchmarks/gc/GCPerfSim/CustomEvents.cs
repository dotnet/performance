using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

[EventSource(Name = "GCPerfsimCustomEvents")]
public class GCPerfsimCustomEvents : EventSource
{
    private const int PrivateMemoryAtGCStartEventId = 1;
    private const int PrivateMemoryAtGCEndEventId = 2;

    public static GCPerfsimCustomEvents Log = new GCPerfsimCustomEvents();

    [Event(PrivateMemoryAtGCStartEventId)]
    public void PrivateMemoryAtGCStart(long PrivateMemory) => WriteEvent(PrivateMemoryAtGCStartEventId, PrivateMemory);

    [Event(PrivateMemoryAtGCEndEventId)]
    public void PrivateMemoryAtGCEnd(long PrivateMemory) => WriteEvent(PrivateMemoryAtGCEndEventId, PrivateMemory);
}

public class GCEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (String.CompareOrdinal(eventSource.Name, "Microsoft-Windows-DotNETRuntime") == 0)
        {
            EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x1); // GC keyword
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (String.CompareOrdinal(eventData.EventName, "GCStart_V2") == 0)
        {
            long privateMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            GCPerfsimCustomEvents.Log.PrivateMemoryAtGCStart(privateMemory);
        }
        if (String.CompareOrdinal(eventData.EventName, "GCEnd_V1") == 0)
        {
            long privateMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            GCPerfsimCustomEvents.Log.PrivateMemoryAtGCEnd(privateMemory);
        }
    }
}