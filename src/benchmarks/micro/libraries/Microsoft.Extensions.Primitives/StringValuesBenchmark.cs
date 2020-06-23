// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MicroBenchmarks;

namespace Microsoft.Extensions.Primitives.Performance
{
    [BenchmarkCategory(Categories.Libraries)]
    public class StringValuesBenchmark
    {
        private readonly string _string = "Hello world!";
        private readonly string[] _stringArray = new[] { "Hello", "world", "!" };
        private readonly StringValues _stringBased = new StringValues("Hello world!");
        private readonly StringValues _arrayBased = new StringValues(new[] { "Hello", "world", "!" });


        [Benchmark]
        public StringValues Ctor_String() => new StringValues(_string);

        [Benchmark]
        public StringValues Ctor_Array() => new StringValues(_stringArray);

        [Benchmark]
        public string Indexer_FirstElement_String() => _stringBased[0];

        [Benchmark]
        public string Indexer_FirstElement_Array() => _arrayBased[0];

        [Benchmark]
        public int Count_String() => _stringBased.Count;

        [Benchmark]
        public int Count_Array() => _arrayBased.Count;

        [Benchmark]
        public int ForEach_String()
        {
            int result = 0;
            foreach (var item in _stringBased)
            {
                result += item.Length;
            }
            return result;
        }

        [Benchmark]
        public int ForEach_Array()
        {
            int result = 0;
            foreach (var item in _arrayBased)
            {
                result += item.Length;
            }
            return result;
        }

    }
}
