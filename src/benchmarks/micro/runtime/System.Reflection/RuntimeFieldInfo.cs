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
    public class RuntimeFieldInfo
    {
        private const int Iterations = 1200;
        private readonly List<FieldInfo> _fields = new(Iterations);

        [GlobalSetup]
        public void Setup()
        {
            var baseType = typeof(RuntimeFieldInfoTestClass);
            var fieldsPerType = baseType.GetFields().Length;

            var assemblyName = new AssemblyName(baseType.Namespace + ".DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            for (var i = 0; i < Iterations; i += fieldsPerType)
            {
                var typeBuilder = moduleBuilder.DefineType($"RuntimeDerivedClass{i}", TypeAttributes.Public, baseType);

                var derivedType = typeBuilder.CreateType();
                _fields.AddRange(derivedType.GetFields());
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void AddToHashSet()
        {
            var set = new HashSet<FieldInfo>();

            for (int i = 0; i < _fields.Count; i++)
            {
                set.Add(_fields[i]);
            }
        }
    }

    public class RuntimeFieldInfoTestClass
    {
        public int Field1;
        public string Field2;
        public DateTime Field3;
    }
}