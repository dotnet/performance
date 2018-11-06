// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type (it shows how bad idea is to use non-generic collections for value types)
    [GenericTypeArguments(typeof(string))] // reference type
    public class IterateForEachNonGeneric<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;
        
        private ArrayList _arraylist;
        private Hashtable _hashtable;
        private Queue _queue;
        private Stack _stack;
        private SortedList _sortedlist;

        [GlobalSetup(Target = nameof(ArrayList))]
        public void SetupArrayList() => _arraylist = new ArrayList(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public object ArrayList()
        {
            object result = default;
            var collection = _arraylist;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Hashtable))]
        public void SetupHashtable() => _hashtable = new Hashtable(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public object Hashtable()
        {
            object result = default;
            var collection = _hashtable;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue() => _queue = new Queue(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public object Queue()
        {
            object result = default;
            var collection = _queue;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack() => _stack = new Stack(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public object Stack()
        {
            object result = default;
            var collection = _stack;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedList))]
        public void SetupSortedList() => _sortedlist = new SortedList(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public object SortedList()
        {
            object result = default;
            var collection = _sortedlist;
            foreach(var item in collection)
                result = item;
            return result;
        }
    }
}