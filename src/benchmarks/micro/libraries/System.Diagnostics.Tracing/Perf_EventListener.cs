// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_EventListener
    {
        private enum TestEnum
        {
            Foo = 123
        }

        private sealed class BenchmarkEventSource : EventSource
        {
            public BenchmarkEventSource() : base(nameof(Perf_EventListener)) { }

            [Event(1)]
            public void EventNoParams() => WriteEvent(1);

            [Event(2)]
            public void EventIntParams(int arg1, int arg2, int arg3) => WriteEvent(2, arg1, arg2, arg3);

            [Event(3)]
            public void EventStringParams(string arg1, string arg2, string arg3) => WriteEvent(3, arg1, arg2, arg3);

            [Event(4)]
#if NET
            [SkipLocalsInit]
#endif
            public unsafe void EventMixedParams(int arg1, string arg2, TestEnum arg3)
            {
                arg2 ??= "";

                fixed (char* arg2Ptr = arg2)
                {
                    const int NumEventDatas = 3;
                    EventData* descrs = stackalloc EventData[NumEventDatas];

                    descrs[0] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg1),
                        Size = sizeof(int)
                    };
                    descrs[1] = new EventData
                    {
                        DataPointer = (IntPtr)(arg2Ptr),
                        Size = (arg2.Length + 1) * sizeof(char)
                    };
                    descrs[2] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg3),
                        Size = sizeof(TestEnum)
                    };

                    WriteEventCore(4, NumEventDatas, descrs);
                }
            }
        }

        private sealed class BenchmarkEventListener : EventListener
        {
            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == nameof(Perf_EventListener))
                {
                    EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.None);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData) { }
        }

        private static readonly BenchmarkEventSource _eventSource = new();
        private static readonly BenchmarkEventListener _eventListener = new();

        [Benchmark]
        public void WriteEvent_NoParams() => _eventSource.EventNoParams();

        [Benchmark]
        public void WriteEvent_IntParams() => _eventSource.EventIntParams(1, 2, 3);

        [Benchmark]
        public void WriteEvent_StringParams() => _eventSource.EventStringParams("foo", "bar", "foobar");

        [Benchmark]
        public void WriteEvent_MixedParams() => _eventSource.EventMixedParams(123, "foo", TestEnum.Foo);
    }
}
