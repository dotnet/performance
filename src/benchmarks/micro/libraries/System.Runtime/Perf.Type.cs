// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Type
    {
        RuntimeTypeHandle TypeHandle = typeof(int).TypeHandle;
        Type Type1 = typeof(int);
        Type Type2 = typeof(string);

        [Benchmark]
        public Type GetTypeFromHandle() => Type.GetTypeFromHandle(TypeHandle);

        [Benchmark]
        public bool op_Equality() => Type1 == Type2;

        public IEnumerable<object> NonFullyQualifiedNamesArguments()
        {
            // Names without AssemblyName:
            yield return "System.Int32"; // elemental type
            yield return "System.Int32*"; // a pointer to an elemental type
            yield return "System.Int32&"; // a reference to an elemental type
            yield return "System.Int32[]"; // single-dimensional array, indexed from 0
            yield return "System.Int32[*]"; // single-dimensional array, but not indexed from 0
            yield return "System.Int32[,]"; // multi-dimensional array
        }

        [Benchmark]
        [ArgumentsSource(nameof(NonFullyQualifiedNamesArguments))]
        public Type GetType_NonFullyQualifiedNames(string input) => Type.GetType(input);

        public IEnumerable<object> FullyQualifiedNamesArguments()
        {
            // We don't return strings here, as they change over the time when assembly versions get increased.
            // This would change benchmark ID and loose historical data tracking.
            yield return typeof(int); // elemental type
            yield return typeof(int).MakePointerType(); // a pointer to an elemental type
            yield return typeof(int).MakeByRefType(); // a reference to an elemental type
            yield return typeof(int[]); // SZArray
            yield return typeof(int).MakeArrayType(1); // single-dimensional array, but not indexed from 0
            yield return typeof(int).MakeArrayType(2); // multi-dimensional array
            yield return typeof(Dictionary<string, bool>); // generic type
            yield return typeof(Dictionary<string, bool>[]); // an array of generic types
            yield return typeof(Nested); // nested type
            yield return typeof(Nested.NestedGeneric<string, bool>); // nested generic type
        }

        [Benchmark]
        [ArgumentsSource(nameof(FullyQualifiedNamesArguments))]
        public Type GetType_FullyQualifiedNames(Type input) => Type.GetType(input.FullName);

        public IEnumerable<object> ResolverArguments()
        {
            yield return typeof(int); // elemental type
            yield return typeof(int[]); // SZArray
            yield return typeof(Nested); // nested type
        }

        [Benchmark]
        [ArgumentsSource(nameof(ResolverArguments))]
        public Type GetType_Resolvers(Type input)
            => Type.GetType(input.FullName, assemblyName => input.Assembly, (assembly, name, ignoreCase) => input);

        [Benchmark]
        public Type GetType_InvalidName() => Type.GetType("Wrong.Syntax[[]]", throwOnError: false);

        public class Nested
        {
            public class NestedGeneric<T1, T2> { }
        }
    }
}
