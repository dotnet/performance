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
        public class ArgumentTypeWrapper
        {
            private readonly string displayName;

            public ArgumentTypeWrapper(Type type, string displayName)
            {
                Type = type;
                this.displayName = displayName;
            }

            public Type Type { get; }
            public override string ToString() => displayName;
        }

        public IEnumerable<object> TypeArguments()
        {
            // We use a wrapper type for each argument to ensure that the test name remains the same into the future
            yield return new ArgumentTypeWrapper(typeof(int), "typeof(int)"); // elemental type
            yield return new ArgumentTypeWrapper(typeof(int).MakePointerType(), "typeof(System.Int32*)"); // a pointer to an elemental type
            yield return new ArgumentTypeWrapper(typeof(int).MakeByRefType(), "typeof(System.Int32&)"); // a reference to an elemental type
            yield return new ArgumentTypeWrapper(typeof(int[]), "typeof(System.Int32[])"); // SZArray
            yield return new ArgumentTypeWrapper(typeof(int).MakeArrayType(1), "typeof(System.Int32[*])"); // single-dimensional array, but not indexed from 0
            yield return new ArgumentTypeWrapper(typeof(int).MakeArrayType(2), "typeof(System.Int32[,])"); // multi-dimensional array
            yield return new ArgumentTypeWrapper(typeof(Dictionary<string, bool>), "typeof(System.Collections.Generic.Dictionary<String, Boolean>)"); // generic type
            yield return new ArgumentTypeWrapper(typeof(Dictionary<string, bool>[]), "typeof(System.Collections.Generic.Dictionary`2[])"); // an array of generic types
            yield return new ArgumentTypeWrapper(typeof(Nested), "typeof(System.Reflection.Metadata.Nested)"); // nested type
            yield return new ArgumentTypeWrapper(typeof(Nested.NestedGeneric<string, bool>), "typeof(System.Reflection.Metadata.NestedGeneric<String, Boolean>)"); // nested generic type
            yield return new ArgumentTypeWrapper(typeof(Dictionary<List<int[]>[,], List<int?[][][,]>>[]), "typeof(System.Collections.Generic.Dictionary`2[]) (COMPLEX)"); // complex generic type (node count = 16)
        }

        [Benchmark]
        [ArgumentsSource(nameof(TypeArguments))]
        public TypeName Parse_FullNames(ArgumentTypeWrapper input) => TypeName.Parse(input.Type.FullName);

        [Benchmark]
        [ArgumentsSource(nameof(TypeArguments))]
        public TypeName Parse_AssemblyQualifiedName(ArgumentTypeWrapper input) => TypeName.Parse(input.Type.AssemblyQualifiedName);

        // The Name, FullName and AssemblyQualifiedName properties are lazy and cached,
        // so we need to parse a new TypeName instance in order to get these properties calculated.
        [Benchmark]
        [ArgumentsSource(nameof(TypeArguments))]
        public string ParseAndGetFullName(ArgumentTypeWrapper input) => TypeName.Parse(input.Type.FullName).FullName;

        [Benchmark]
        [ArgumentsSource(nameof(TypeArguments))]
        public string ParseAndGetAssemblyQualifiedName(ArgumentTypeWrapper input) => TypeName.Parse(input.Type.AssemblyQualifiedName).AssemblyQualifiedName;

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
