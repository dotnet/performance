using PerfLabGenericEventSourceForwarder;

sealed class StartupHook
{
    public static LTTngForwardingEventListener Listener;

    public static void Initialize()
    {
        Listener = new LTTngForwardingEventListener();
    }
}
