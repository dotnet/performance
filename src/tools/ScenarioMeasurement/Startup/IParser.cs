// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Reporting;

namespace ScenarioMeasurement
{
    public interface IParser
    {
        void EnableUserProviders(ITraceSession user);
        void EnableKernelProvider(ITraceSession kernel);
        IEnumerable<Counter> Parse(string mergeTraceFile, string processName, IList<int> pids, string commandLine);
    }
}
