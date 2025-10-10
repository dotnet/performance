// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Reflection
{
    [BenchmarkCategory(Categories.Runtime, Categories.Reflection)]
    public class RuntimeEventInfo
    {
        private static readonly EventInfo s_eventInfo = typeof(RuntimeEventInfoTestClass).GetEvent(nameof(RuntimeEventInfoTestClass.Event1));

        [Benchmark]
        public int GetHashCodeBenchmark()
        {
            return s_eventInfo.GetHashCode();
        }
    }

    public class RuntimeEventInfoTestClass
    {
        public event EventHandler Event1;
        protected virtual void OnEvent1() => Event1?.Invoke(this, EventArgs.Empty);
    }
}