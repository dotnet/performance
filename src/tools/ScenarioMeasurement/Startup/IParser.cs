using Microsoft.Diagnostics.Tracing.Session;
using System.Collections.Generic;
using Reporting;

namespace ScenarioMeasurement
{
    internal interface IParser
    {
        void EnableUserProviders(TraceEventSession user);
        void EnableKernelProvider(TraceEventSession kernel);
        IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids);
    }
}
