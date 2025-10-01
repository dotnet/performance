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
    public class RuntimePropertyInfo
    {
        private const int Iterations = 1200;
        private readonly List<PropertyInfo> _properties = new(Iterations);

        [GlobalSetup]
        public void Setup()
        {
            var baseType = typeof(RuntimePropertyInfoTestClass);
            var propertiesPerType = baseType.GetProperties().Length;

            var assemblyName = new AssemblyName(baseType.Namespace + ".DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            for (var i = 0; i < Iterations; i += propertiesPerType)
            {
                var typeBuilder = moduleBuilder.DefineType($"RuntimeDerivedClass{i}", TypeAttributes.Public, baseType);

                var derivedType = typeBuilder.CreateType();
                _properties.AddRange(derivedType.GetProperties());
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void AddToHashSet()
        {
            var set = new HashSet<PropertyInfo>();

            for (int i = 0; i < _properties.Count; i++)
            {
                set.Add(_properties[i]);
            }
        }
    }

    public class RuntimePropertyInfoTestClass
    {
        public int Property1 { get; set; }
        public string Property2 { get; set; }
        public DateTime Property3 { get; set; }
    }
}