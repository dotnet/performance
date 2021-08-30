// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.ObjectModel;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections)]
    public class Perf_ObservableCollection
    {
        private readonly ObservableCollection<int> _collection = new ObservableCollection<int>();

        [Benchmark]
        public void ClearAdd()
        {
            _collection.Clear();
            for (int i = 0; i < 100; i++)
            {
                _collection.Add(i);
            }
        }
    }
}
