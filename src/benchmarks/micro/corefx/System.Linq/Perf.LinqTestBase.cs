// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Engines;

namespace System.Linq.Tests
{
    /// <summary>
    /// Classes and methods to unify performance testing logic
    /// </summary>
    public class Perf_LinqTestBase
    {
        public class LinqTestData
        {
            public LinqTestData(IEnumerable<int> collection) => Collection = collection;

            public IEnumerable<int> Collection { get; }

            // the value returned by ToString is used in the text representation of Benchmark ID in our reporting system
            public override string ToString()
            {
                switch (Collection)
                {
                    case int[] _:
                        return "int[]";
                    case List<int> _:
                        return "List<int>";
                    case IList<int> _:
                        return "IList<int>";
                    case IOrderedEnumerable<int> _:
                        return "IOrderedEnumerable<int>";
                    default:
                        return "IEnumerable<int>";
                }
            }
        }

        public class EnumerableWrapper<T> : IEnumerable<T>
        {
            private readonly T[] _array;
            public EnumerableWrapper(T[] array) { _array = array; }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private T[] _array;
            public ReadOnlyCollectionWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class ReadOnlyListWrapper<T> : IReadOnlyList<T>
        {
            private T[] _array;
            public ReadOnlyListWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }
            public T this[int index] { get { return _array[index]; } }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class CollectionWrapper<T> : ICollection<T>
        {
            private T[] _array;
            public CollectionWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }
            public bool IsReadOnly { get { return true; } }
            public bool Contains(T item)
            {
                return Array.IndexOf(_array, item) >= 0;
            }
            public void CopyTo(T[] array, int arrayIndex)
            {
                _array.CopyTo(array, arrayIndex);
            }

            public void Add(T item) {  throw new NotImplementedException(); }
            public void Clear()  {  throw new NotImplementedException(); }
            public bool Remove(T item) { throw new NotImplementedException(); }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class IListWrapper<T> : IList<T>
        {
            private T[] _array;
            public IListWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }
            public bool IsReadOnly { get { return true; } }
            public T this[int index]
            {
                get { return _array[index]; }
                set { throw new NotImplementedException(); }
            }
            public bool Contains(T item)
            {
                return Array.IndexOf(_array, item) >= 0;
            }
            public void CopyTo(T[] array, int arrayIndex)
            {
                _array.CopyTo(array, arrayIndex);
            }
            public int IndexOf(T item)
            {
                return Array.IndexOf(_array, item);
            }

            public void Add(T item) { throw new NotImplementedException(); }
            public void Clear() { throw new NotImplementedException(); }
            public bool Remove(T item) { throw new NotImplementedException(); }
            public void Insert(int index, T item) { throw new NotImplementedException(); }
            public void RemoveAt(int index) { throw new NotImplementedException(); }


            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }
        
        public enum WrapperType
        {
            NoWrap,
            IEnumerable,
            IReadOnlyCollection,
            IReadOnlyList,
            ICollection,
            IList
        }

        /// <summary>
        /// Wrap array with one of wrapper types
        /// </summary>
        public static IEnumerable<T> Wrap<T>(T[] source, WrapperType wrapperKind)
        {
            switch (wrapperKind)
            {
                case WrapperType.NoWrap:
                    return source;
                case WrapperType.IEnumerable:
                    return new EnumerableWrapper<T>(source);
                case WrapperType.ICollection:
                    return new CollectionWrapper<T>(source);
                case WrapperType.IReadOnlyCollection:
                    return new ReadOnlyCollectionWrapper<T>(source);
                case WrapperType.IReadOnlyList:
                    return new ReadOnlyListWrapper<T>(source);
                case WrapperType.IList:
                    return new IListWrapper<T>(source);
            }

            return source;
        }

        /// <summary>
        /// Main method to measure performance.
        /// Creates array of Int32 with length 'elementCount', wraps it by one of the wrapper, applies LINQ and measures materialization to Array
        /// </summary>
        public static void Measure(int[] data, WrapperType wrapperKind, Func<IEnumerable<int>, IEnumerable<int>> applyLINQ, Consumer consumer)
        {
            IEnumerable<int> wrapper = Wrap(data, wrapperKind);

            applyLINQ(wrapper).Consume(consumer); // we use BDN utility to consume LINQ query
        }
    }
}
