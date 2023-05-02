// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Struct
{
    [BenchmarkCategory(Categories.Runtime, Categories.JIT)]
    public class GSeq
    {
        private int[] _array;

        [GlobalSetup]
        public void Setup()
        {
            _array = Enumerable.Range(0, 10000).ToArray();
        }

        [Benchmark]
        public int FilterSkipMapSum()
        {
            var arr = new ArrayEnumerator<int>(_array);
            var filter = new FilterEnumerator<int, ArrayEnumerator<int>, FilterIsOdd>(arr, new FilterIsOdd());
            var skip = new SkipEnumerator<int, FilterEnumerator<int, ArrayEnumerator<int>, FilterIsOdd>>(5, filter);
            var map =
                new MapEnumerator<int, int,
                    SkipEnumerator<int, FilterEnumerator<int, ArrayEnumerator<int>, FilterIsOdd>>, Plus15Mapper>(
                    skip,
                    new Plus15Mapper());
            return Fold2<int, int,
                MapEnumerator<int, int,
                    SkipEnumerator<int, FilterEnumerator<int, ArrayEnumerator<int>, FilterIsOdd>>,
                    Plus15Mapper>, FoldPlus>(map, 0, new FoldPlus());
        }

        public static TOut Fold2<TIn, TOut, TEnumerator, TFolder>(TEnumerator enumerator, TOut initial,
            TFolder folder)
            where TEnumerator : struct, IEnumerator<TIn>
            where TFolder : struct, IInvokable<TOut, TIn, TOut>
        {
            while (enumerator.MoveNext())
                initial = folder.Invoke(initial, enumerator.Current);

            return initial;
        }
    }

    public interface IInvokable<TIn1, TOut>
    {
        TOut Invoke(TIn1 value);
    }

    public interface IInvokable<TIn1, TIn2, TOut>
    {
        TOut Invoke(TIn1 value1, TIn2 value2);
    }


    public readonly struct Plus15Mapper : IInvokable<int, int>
    {
        public int Invoke(int value) => value + 15;
    }

    public readonly struct FilterIsOdd : IInvokable<int, bool>
    {
        public bool Invoke(int value) => value % 2 == 1;
    }

    public readonly struct FoldPlus : IInvokable<int, int, int>
    {
        public int Invoke(int value1, int value2) => value1 + value2;
    }

    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _array;
        private int _count;

        public ArrayEnumerator(T[] array)
        {
            _array = array;
            _count = -1;
        }


        public bool MoveNext()
        {
            _count++;
            return (uint)_count < (uint)_array.Length;
        }

        public void Reset()
        {
        }

        public T Current => _array[_count];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    public struct SkipEnumerator<T, TEnumerator> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private int _skipCount;

        public SkipEnumerator(int skipCount, TEnumerator enumerator)
        {
            _skipCount = skipCount;
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            while (_skipCount > 0)
            {
                if (!_enumerator.MoveNext())
                    return false;
                _skipCount--;
            }

            return _enumerator.MoveNext();
        }

        public void Reset()
        {
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    public struct MapEnumerator<T, TOut, TEnumerator, TMapper> : IEnumerator<TOut>
        where TEnumerator : struct, IEnumerator<T>
        where TMapper : struct, IInvokable<T, TOut>
    {
        private TEnumerator _enumerator;
        private TMapper _mapper;

        public MapEnumerator(TEnumerator enumerator, TMapper mapper)
        {
            _enumerator = enumerator;
            _mapper = mapper;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
        }

        public TOut Current => _mapper.Invoke(_enumerator.Current);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    public struct FilterEnumerator<T, TEnumerator, TFilter> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
        where TFilter : struct, IInvokable<T, bool>
    {
        private TEnumerator _enumerator;
        private TFilter _filter;

        public FilterEnumerator(TEnumerator enumerator, TFilter filter)
        {
            _enumerator = enumerator;
            _filter = filter;
        }

        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                if (_filter.Invoke(_enumerator.Current))
                    return true;
            }

            return false;
        }

        public void Reset()
        {
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

}
