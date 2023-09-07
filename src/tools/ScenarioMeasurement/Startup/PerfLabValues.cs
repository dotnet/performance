namespace ScenarioMeasurement;

// see src\scenarios\staticdeps\PerfLab.cs
class PerfLabValues
{
    // to "prevent" event id collisions
    private const int MagicConstant = 6666;
    public const string EventSourceName = "PerfLabGenericEventSource";
    public const string StartupEventName = "Startup";
    public const int StartupEventId = MagicConstant + 1;
    public const string OnMainEventName = "OnMain";
    public const int OnMainEventId = MagicConstant + 2;

    public const string ForwarderName = "PerfLabGenericEventSourceForwarder";
    public const string LTTngProviderName = "PerfLabGenericEventSourceLTTngProvider";
    public const string LTTngProviderLibraryName = LTTngProviderName;
}
