// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Reflection.Metadata
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_TypeName
    {
        public IEnumerable<object> TypeArguments()
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
            yield return typeof(Dictionary<List<int[]>[,], List<int?[][][,]>>[]); // complex generic type (node count = 16)
        }

        [Benchmark]
        [ArgumentsSource(nameof(TypeArguments))]
        public TypeName Parse_FullNames(Type input) => TypeName.Parse(input.FullName);

        // The FullName property is lazy and cached, so we need to parse a new TypName instance
        // in order to get the FullName property calculated.
        [Benchmark]
        [ArgumentsSource(nameof(TypeArguments))]
        public string ParseAndGetFullName(Type input) => TypeName.Parse(input.FullName).FullName;

        [Benchmark]
        [ArgumentsSource(nameof(TypeArguments))]
        public TypeName Parse_AssemblyQualifiedName(Type input) => TypeName.Parse(input.AssemblyQualifiedName);

        public IEnumerable<string> InvalidArguments()
        {
            yield return "Wrong.Syntax[[]]";
        }

        [Benchmark]
        [ArgumentsSource(nameof(InvalidArguments))]
        public bool TryParse_Invalid(string input) => TypeName.TryParse(input, out _);

        public class Nested
        {
            public class NestedGeneric<T1, T2> { }
        }
    }
}
