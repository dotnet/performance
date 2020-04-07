// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.NonGenericCollections)]
    public class CtorGivenSizeNonGeneric
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;
        
        [Benchmark]
        public ArrayList ArrayList() => new ArrayList(Size);

        [Benchmark]
        public Hashtable Hashtable() => new Hashtable(Size);

        [Benchmark]
        public Queue Queue() => new Queue(Size);

        [Benchmark]
        public Stack Stack() => new Stack(Size);

        [Benchmark]
        public SortedList SortedList() => new SortedList(Size);
    }
}