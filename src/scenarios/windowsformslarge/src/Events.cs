using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinformsNetCorePerfApp1
{
    [EventSource(Guid = "9bb228bd-1033-5cf0-1a56-c2dbbe0ebc86")]
    class PerfLabGenericEventSource : EventSource
    {
        public static PerfLabGenericEventSource Log = new PerfLabGenericEventSource();
        public void Startup() => WriteEvent(1);
    }
}