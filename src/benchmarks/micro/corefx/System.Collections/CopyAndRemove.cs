
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CopyAndRemove<T>
    {
        private T[] _items;
        
        private List<T> _list;
        private LinkedList<T> _linkedList;
        private HashSet<T> _hashSet;
        private SortedSet<T> _sortedSet;
        private Dictionary<T, T> _dictionary;
        private SortedList<T, T> _sortedList;
        private SortedDictionary<T, T> _sortedDictionary;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            T[] twoParts = ValuesGenerator.ArrayOfUniqueValues<T>(Size * 2);
            T[] part1 = twoParts.Take(Size).ToArray();
            T[] part2 = twoParts.Skip(Size).ToArray();

            _items = part1;
            _list = new List<T>(_items);
            _linkedList = new LinkedList<T>(_items);
            _hashSet = new HashSet<T>(_items);
            _sortedSet = new SortedSet<T>(_items);
            _dictionary = new Dictionary<T, T>();
            _sortedList = new SortedList<T, T>();
            _sortedDictionary = new SortedDictionary<T, T>();

            foreach (var value in _items)
            {
                _dictionary.Add(value, value);
                _sortedList.Add(value, value);
                _sortedDictionary.Add(value, value);
            }

            Array.Sort(part2, _items);
        }

        [Benchmark]
        public bool List()
        {
            bool result = true;
            T[] items = _items;
            List<T> copy = new List<T>(_list);

            for (int i = 0; i < items.Length; i++)
                result &= copy.Remove(items[i]);

            if (!result)
                throw new InvalidOperationException("All items must be in the collection.");

            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = true;
            T[] items = _items;
            LinkedList<T> copy = new LinkedList<T>(_linkedList);

            for (int i = 0; i < items.Length; i++)
                result &= copy.Remove(items[i]);

            if (!result)
                throw new InvalidOperationException("All items must be in the collection.");

            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = true;
            T[] items = _items;
            HashSet<T> copy = new HashSet<T>(_hashSet);

            for (int i = 0; i < items.Length; i++)
                result &= copy.Remove(items[i]);

            if (!result)
                throw new InvalidOperationException("All items must be in the collection.");

            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = true;
            T[] items = _items;
            SortedSet<T> copy = new SortedSet<T>(_sortedSet);

            for (int i = 0; i < items.Length; i++)
                result &= copy.Remove(items[i]);

            if (!result)
                throw new InvalidOperationException("All items must be in the collection.");

            return result;
        }

        [Benchmark]
        public bool Dictionary()
        {
            bool result = true;
            T[] items = _items;
            Dictionary<T, T> copy = new Dictionary<T, T>(_dictionary);

            for (int i = 0; i < items.Length; i++)
                result &= copy.Remove(items[i]);

            if (!result)
                throw new InvalidOperationException("All items must be in the collection.");

            return result;
        }

        [Benchmark]
        public bool SortedList()
        {
            bool result = true;
            T[] items = _items;
            SortedList<T, T> copy = new SortedList<T, T>(_sortedList);

            for (int i = 0; i < items.Length; i++)
                result &= copy.Remove(items[i]);

            if (!result)
                throw new InvalidOperationException("All items must be in the collection.");

            return result;
        }

        [Benchmark]
        public bool SortedDictionary()
        {
            bool result = true;
            T[] items = _items;
            SortedDictionary<T, T> copy = new SortedDictionary<T, T>(_sortedDictionary);

            for (int i = 0; i < items.Length; i++)
                result &= copy.Remove(items[i]);

            if (!result)
                throw new InvalidOperationException("All items must be in the collection.");

            return result;
        }
    }
}