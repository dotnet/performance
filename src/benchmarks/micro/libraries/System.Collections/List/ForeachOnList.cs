// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace MicroBenchmarks.libraries.System.Collections.List
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    public class Perf_ForeachOnList
    {
        private int[] _array;

        private List<int> _list;

        private IEnumerable<int> _listAsIEnumerable;

        [GlobalSetup]
        public void InitializeValue()
        {
            _array = ValuesGenerator.Array<int>(3_000);
            _list = _array.ToList();
            _listAsIEnumerable = _list;
        }

        [Benchmark(Baseline = true)]
        public int ForeachOnArray()
        {
            var sum = 0;

            foreach (var i in _array)
            {
                sum += i;
            }

            return sum;
        }

        [Benchmark]
        public int ForeachOnList()
        {
            var sum = 0;

            foreach (var i in _list)
            {
                sum += i;
            }

            return sum;
        }

        [Benchmark]
        public int ForeachOnListAsIEnumerable()
        {
            var sum = 0;

            foreach (var i in _listAsIEnumerable)
            {
                sum += i;
            }

            return sum;
        }
    }
}
