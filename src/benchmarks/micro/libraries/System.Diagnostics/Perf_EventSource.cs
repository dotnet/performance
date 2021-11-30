// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Diagnostics.Tracing;

namespace System.Diagnostics
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_EventSource
    {
        private BenchmarkEventListener _listener;

        [GlobalSetup]
        public void Setup() => _listener = new BenchmarkEventListener();

        [GlobalCleanup]
        public void Cleanup() => _listener.Dispose();

        [Benchmark]
        public void Log()
        {
            BenchmarkEventSource.Log.NoArgs();
            BenchmarkEventSource.Log.MultipleArgs("hello", 6, 0);
        }

        private sealed class BenchmarkEventListener : EventListener
        {
            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource is BenchmarkEventSource)
                {
                    EnableEvents(eventSource, EventLevel.LogAlways);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData) { }
        }

        private sealed class BenchmarkEventSource : EventSource
        {
            public static readonly BenchmarkEventSource Log = new BenchmarkEventSource();

            [Event(1)]
            public void NoArgs() => WriteEvent(1);

            [Event(2)]
            public void MultipleArgs(string arg1, int arg2, int arg3) => WriteEvent(2, arg1, arg2, arg3);
        }
    }
}
