using System.Diagnostics.Tracing;
using ScenarioMeasurement;

namespace PerfLabGenericEventSourceForwarder;

sealed class LTTngForwardingEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (PerfLabValues.EventSourceName.Equals(eventSource.Name, StringComparison.Ordinal))
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (PerfLabValues.StartupEventName.Equals(eventData.EventName, StringComparison.Ordinal))
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            Native.EmitStartup();
        }
        else if (PerfLabValues.OnMainEventName.Equals(eventData.EventName, StringComparison.Ordinal))
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            Native.EmitOnMain();
        }
    }
}
