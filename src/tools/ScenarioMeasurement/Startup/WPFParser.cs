using Reporting;
using System.Collections.Generic;

namespace ScenarioMeasurement
{
    public class WPFParser : IParser
    {
        public void EnableKernelProvider(ITraceSession kernel)
        {
            throw new System.NotImplementedException();
        }

        public void EnableUserProviders(ITraceSession user)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine)
        {
            throw new System.NotImplementedException();
        }
    }
}
