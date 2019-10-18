
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
    public class RemoveFalse<T>
    {
        private T[] _notFound;
        
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

            _list = new List<T>(part1);
            _linkedList = new LinkedList<T>(part1);
            _hashSet = new HashSet<T>(part1);
            _sortedSet = new SortedSet<T>(part1);
            _dictionary = new Dictionary<T, T>();
            _sortedList = new SortedList<T, T>();
            _sortedDictionary = new SortedDictionary<T, T>();

            foreach (var value in part1)
            {
                _dictionary.Add(value, value);
                _sortedList.Add(value, value);
                _sortedDictionary.Add(value, value);
            }

            _notFound = part2;
        }

        [Benchmark]
        public bool List()
        {
            bool result = false;
            T[] notFound = _notFound;

            for (int i = 0; i < notFound.Length; i++)
                result |= _list.Remove(notFound[i]);

            if (result)
                throw new InvalidOperationException("None of the notFound should be in the collection.");

            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = false;
            T[] notFound = _notFound;

            for (int i = 0; i < notFound.Length; i++)
                result |= _linkedList.Remove(notFound[i]);

            if (result)
                throw new InvalidOperationException("None of the notFound should be in the collection.");

            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = false;
            T[] notFound = _notFound;

            for (int i = 0; i < notFound.Length; i++)
                result |= _hashSet.Remove(notFound[i]);

            if (result)
                throw new InvalidOperationException("None of the notFound should be in the collection.");

            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = false;
            T[] notFound = _notFound;

            for (int i = 0; i < notFound.Length; i++)
                result |= _sortedSet.Remove(notFound[i]);

            if (result)
                throw new InvalidOperationException("None of the notFound should be in the collection.");

            return result;
        }

        [Benchmark]
        public bool Dictionary()
        {
            bool result = false;
            T[] notFound = _notFound;

            for (int i = 0; i < notFound.Length; i++)
                result |= _dictionary.Remove(notFound[i]);

            if (result)
                throw new InvalidOperationException("None of the notFound should be in the collection.");

            return result;
        }

        [Benchmark]
        public bool SortedList()
        {
            bool result = false;
            T[] notFound = _notFound;

            for (int i = 0; i < notFound.Length; i++)
                result |= _sortedList.Remove(notFound[i]);

            if (result)
                throw new InvalidOperationException("None of the notFound should be in the collection.");

            return result;
        }

        [Benchmark]
        public bool SortedDictionary()
        {
            bool result = false;
            T[] notFound = _notFound;

            for (int i = 0; i < notFound.Length; i++)
                result |= _sortedDictionary.Remove(notFound[i]);

            if (result)
                throw new InvalidOperationException("None of the notFound should be in the collection.");

            return result;
        }
    }
}