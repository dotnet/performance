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
    public class RuntimeMethodInfo
    {
        private const int Iterations = 1200;
        private readonly List<MethodInfo> _methods = new(Iterations);

        [GlobalSetup]
        public void Setup()
        {
            var baseType = typeof(RuntimeMethodInfoTestClass);
            var methodsPerType = baseType.GetMethods().Length;

            var assemblyName = new AssemblyName(baseType.Namespace + ".DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            for (var i = 0; i < Iterations; i += methodsPerType)
            {
                var typeBuilder = moduleBuilder.DefineType($"RuntimeDerivedClass{i}", TypeAttributes.Public, baseType);

                var derivedType = typeBuilder.CreateType();
                _methods.AddRange(derivedType.GetMethods());
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _methods.Clear();
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public HashSet<MethodInfo> AddToHashSet()
        {
            var set = new HashSet<MethodInfo>();

            for (int i = 0; i < _methods.Count; i++)
            {
                set.Add(_methods[i]);
            }

            return set;
        }
    }

    public class RuntimeMethodInfoTestClass
    {
        public int Method1() => throw new NotImplementedException();
        public string Method2() => throw new NotImplementedException();
        public DateTime Method3() => throw new NotImplementedException();
    }
}