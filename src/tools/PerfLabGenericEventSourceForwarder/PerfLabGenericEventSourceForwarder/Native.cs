using System.Runtime.InteropServices;
using ScenarioMeasurement;

namespace PerfLabGenericEventSourceForwarder;

static class Native
{
    [DllImport(PerfLabValues.LTTngProviderLibraryName, EntryPoint = "emit_startup")]
    public static extern void EmitStartup();

    [DllImport(PerfLabValues.LTTngProviderLibraryName, EntryPoint = "emit_on_main")]
    public static extern void EmitOnMain();
}
