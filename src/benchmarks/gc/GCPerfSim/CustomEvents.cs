using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

enum CustomEventID: int
{
    PrivateMemoryAtGCStart = 1,
    PrivateMemoryAtGCEnd = 2
}


[EventSource(Name = "CustomEvents")]
class CustomEvents : EventSource
{
    public static CustomEvents Log = new CustomEvents();

    [Event((int)CustomEventID.PrivateMemoryAtGCStart)]
    public void PrivateMemoryAtGCStart(long PrivateMemory) => WriteEvent((int)CustomEventID.PrivateMemoryAtGCStart, PrivateMemory);

    [Event((int)CustomEventID.PrivateMemoryAtGCEnd)]
    public void PrivateMemoryAtGCEnd(long PrivateMemory) => WriteEvent((int)CustomEventID.PrivateMemoryAtGCEnd, PrivateMemory);
}

class GCEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (String.Compare(eventSource.Name, "Microsoft-Windows-DotNETRuntime") == 0)
        {
            EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x1); // GC keyword
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (String.CompareOrdinal(eventData.EventName, "GCStart_V2") == 0)
        {
            long privateMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            CustomEvents.Log.PrivateMemoryAtGCStart(privateMemory);
        }
        if (String.CompareOrdinal(eventData.EventName, "GCEnd_V1") == 0)
        {
            long privateMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            CustomEvents.Log.PrivateMemoryAtGCEnd(privateMemory);
        }
    }
}