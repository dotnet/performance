// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Reflection
{
    [BenchmarkCategory(Categories.Runtime, Categories.Reflection)]
    public class RuntimeEventInfo
    {
        private const int Iterations = 1200;
        private readonly List<EventInfo> _events = new(Iterations);

        [GlobalSetup]
        public void Setup()
        {
            var baseType = typeof(RuntimeEventInfoTestClass);
            var eventsPerType = baseType.GetEvents().Length;

            var assemblyName = new AssemblyName(baseType.Namespace + ".DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            for (var i = 0; i < Iterations; i += eventsPerType)
            {
                var typeBuilder = moduleBuilder.DefineType($"RuntimeDerivedClass{i}", TypeAttributes.Public, baseType);

                var derivedType = typeBuilder.CreateType();
                _events.AddRange(derivedType.GetEvents());
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void AddToHashSet()
        {
            var set = new HashSet<EventInfo>();

            for (int i = 0; i < _events.Count; i++)
            {
                set.Add(_events[i]);
            }
        }
    }

    public class RuntimeEventInfoTestClass
    {
        public event EventHandler event1;
        public event EventHandler event2;
        public event EventHandler event3;

        protected virtual void OnEvent1() => event1?.Invoke(this, EventArgs.Empty);

        protected virtual void OnEvent2() => event2?.Invoke(this, EventArgs.Empty);

        protected virtual void OnEvent3() => event3?.Invoke(this, EventArgs.Empty);
    }
}