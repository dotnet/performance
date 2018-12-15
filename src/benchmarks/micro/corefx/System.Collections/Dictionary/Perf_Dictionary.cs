// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    public class Perf_Dictionary
    {
        [Params(3_000)]
        public int Items;

        private Dictionary<int, int> _dict;

        [GlobalSetup(Target = nameof(ContainsValue))]
        public void InitializeContainsValue()
        {
            _dict = Enumerable.Range(0, 3_000).ToDictionary(i => i);
        }

        [Benchmark]
        public int ContainsValue()
        {
            Dictionary<int, int> d = _dict;
            int count = 0;

            for (int i = 0; i < Items; i++)
            {
                if (d.ContainsValue(i))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
