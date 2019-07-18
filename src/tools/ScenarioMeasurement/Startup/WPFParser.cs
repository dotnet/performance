using Microsoft.Diagnostics.Tracing.Session;
using Reporting;
using System.Collections.Generic;

namespace ScenarioMeasurement
{
    internal class WPFParser : IParser
    {
        public void EnableKernelProvider(TraceEventSession kernel)
        {
            throw new System.NotImplementedException();
        }

        public void EnableUserProviders(TraceEventSession user)
        {
            throw new System.NotImplementedException();
        }

        public IList<Counter> Parse(string mergeTraceFile, string processName)
        {
            throw new System.NotImplementedException();
        }
    }
}