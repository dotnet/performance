// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    public class CtorDefaultSizeNonGeneric
    {
        [Benchmark]
        public ArrayList ArrayList() => new ArrayList();

        [Benchmark]
        public Hashtable Hashtable() => new Hashtable();

        [Benchmark]
        public Queue Queue() => new Queue();

        [Benchmark]
        public Stack Stack() => new Stack();

        [Benchmark]
        public SortedList SortedList() => new SortedList();
    }
}