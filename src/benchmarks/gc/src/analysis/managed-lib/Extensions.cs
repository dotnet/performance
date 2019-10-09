// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace GCPerf
{
    static class Extensions
    {
        public static StackSource CPUStacks(
            this Microsoft.Diagnostics.Tracing.Etlx.TraceLog eventLog,
            Microsoft.Diagnostics.Tracing.Etlx.TraceProcess process = null,
            Predicate<TraceEvent> predicate = null)
        {
            Microsoft.Diagnostics.Tracing.Etlx.TraceEvents events;

            if (process == null)
                events = eventLog.Events.Filter((x) => ((predicate == null) || predicate(x)) && x is SampledProfileTraceData && x.ProcessID != 0);
            else
                events = process.EventsInProcess.Filter((x) => ((predicate == null) || predicate(x)) && x is SampledProfileTraceData);

            var traceStackSource = new TraceEventStackSource(events);
            traceStackSource.ShowUnknownAddresses = true;

            // Clone the samples so that the caller doesn't have to go back to the ETL file from here on.
            return CopyStackSource.Clone(traceStackSource);
        }
    }
}
