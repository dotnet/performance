// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
    public class Perf_Array
    {
        private Array _arr1;
        private Array _arr2;
        private Array _arr3;
        private Array _destinationArray;
        private byte[][] _byteArrays;
        private int[] _reversibleArray;
        private char[] _indexOfCharArray;
        private short[] _indexOfShortArray;
        private byte[] _clearableArray;

        private const int MAX_ARRAY_SIZE = 4096;

        private const int OldSize = 42;
        private const int NewSize = 41;
        private const int ByteArraysCount = 2_000_000;

        private static readonly int s_DIM_1 = MAX_ARRAY_SIZE;
        private static readonly int s_DIM_2 = (int) Math.Pow(MAX_ARRAY_SIZE, (1.0 / 2.0));
        private static readonly int s_DIM_3 = (int) (Math.Pow(MAX_ARRAY_SIZE, (1.0 / 3.0)) + .001);

        [Benchmark]
        public Array ArrayCreate1D() => Array.CreateInstance(typeof(int), s_DIM_1);

        [Benchmark]
        public Array ArrayCreate2D() => Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);

        [Benchmark]
        public Array ArrayCreate3D() => Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);

        [GlobalSetup(Target = nameof(ArrayAssign1D))]
        public void SetupArrayAssign1D() => _arr1 = Array.CreateInstance(typeof(int), s_DIM_1);

        [Benchmark]
        public void ArrayAssign1D()
        {
            Array arr1 = _arr1;

            for (int j = 0; j < s_DIM_1; j++)
            {
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
                arr1.SetValue(j, j);
            }
        }

        [GlobalSetup(Target = nameof(ArrayAssign2D))]
        public void SetupArrayAssign2D() => _arr2 = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);

        [Benchmark]
        public void ArrayAssign2D()
        {
            Array arr2 = _arr2;

            for (int j = 0; j < s_DIM_2; j++)
            {
                for (int k = 0; k < s_DIM_2; k++)
                {
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                    arr2.SetValue(j + k, j, k);
                }
            }
        }

        [GlobalSetup(Target = nameof(ArrayAssign3D))]
        public void SetupArrayAssign3D() => _arr3 = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);

        [Benchmark]
        public void ArrayAssign3D()
        {
            Array arr3 = _arr3;

            for (int j = 0; j < s_DIM_3; j++)
            {
                for (int k = 0; k < s_DIM_3; k++)
                {
                    for (int l = 0; l < s_DIM_3; l++)
                    {
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                        arr3.SetValue(j + k + l, j, k, l);
                    }
                }
            }
        }

        [GlobalSetup(Target = nameof(ArrayRetrieve1D))]
        public void SetupArrayRetrieve1D()
        {
            _arr1 = Array.CreateInstance(typeof(int), s_DIM_1);

            for (int i = 0; i < s_DIM_1; i++)
                _arr1.SetValue(i, i);
        }

        [Benchmark]
        public int ArrayRetrieve1D()
        {
            Array arr1 = _arr1;
            int value = default;

            for (int j = 0; j < s_DIM_1; j++)
            {
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
                value += (int)arr1.GetValue(j);
            }

            return value;
        }

        [GlobalSetup(Target = nameof(ArrayRetrieve2D))]
        public void SetupArrayRetrieve2D()
        {
            _arr2 = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);

            for (int i = 0; i < s_DIM_2; i++)
            {
                for (int j = 0; j < s_DIM_2; j++)
                    _arr2.SetValue(i + j, i, j);
            }
        }

        [Benchmark]
        public int ArrayRetrieve2D()
        {
            Array arr2 = _arr2;
            int value = default;

            for (int j = 0; j < s_DIM_2; j++)
            {
                for (int k = 0; k < s_DIM_2; k++)
                {
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                    value += (int)arr2.GetValue(j, k);
                }
            }

            return value;
        }

        [GlobalSetup(Target = nameof(ArrayRetrieve3D))]
        public void SetupArrayRetrieve3D()
        {
            _arr3 = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);

            for (int i = 0; i < s_DIM_3; i++)
            {
                for (int j = 0; j < s_DIM_3; j++)
                {
                    for (int k = 0; k < s_DIM_3; k++)
                        _arr3.SetValue(i + j + k, i, j, k);
                }
            }
        }

        [Benchmark]
        public int ArrayRetrieve3D()
        {
            Array arr3 = _arr3;
            int value = default;

            for (int j = 0; j < s_DIM_3; j++)
            {
                for (int k = 0; k < s_DIM_3; k++)
                {
                    for (int l = 0; l < s_DIM_3; l++)
                    {
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                        value += (int)arr3.GetValue(j, k, l);
                    }
                }
            }

            return value;
        }

        [GlobalSetup(Target = nameof(ArrayCopy2D))]
        public void SetupArrayCopy2D()
        {
            _destinationArray = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);
            _arr2 = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);

            for (int i = 0; i < s_DIM_2; i++)
            {
                for (int j = 0; j < s_DIM_2; j++)
                    _arr2.SetValue(i + j, i, j);
            }
        }

        [Benchmark]
        public void ArrayCopy2D() => Array.Copy(_arr2, _destinationArray, s_DIM_2 * s_DIM_2);

        [GlobalSetup(Target = nameof(ArrayCopy3D))]
        public void SetupArrayCopy3D()
        {
            _destinationArray = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);
            _arr3 = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);

            for (int i = 0; i < s_DIM_3; i++)
            {
                for (int j = 0; j < s_DIM_3; j++)
                {
                    for (int k = 0; k < s_DIM_3; k++)
                    {
                        _arr3.SetValue(i + j + k, i, j, k);
                    }
                }
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void ArrayCopy3D() => Array.Copy(_arr3, _destinationArray, s_DIM_3 * s_DIM_3 * s_DIM_3);

        [IterationSetup(Target = nameof(ArrayResize))]
        public void SetupArrayResizeIteration()
        {
            _byteArrays = new byte[ByteArraysCount][];
            for (int i = 0; i < _byteArrays.Length; i++)
                _byteArrays[i] = new byte[OldSize];
        }

        [Benchmark(OperationsPerInvoke = ByteArraysCount)]
        public void ArrayResize()
        {
            for (int i = 0; i < _byteArrays.Length; i++)
                Array.Resize<byte>(ref _byteArrays[i], NewSize);
        }

        [GlobalSetup(Target = nameof(Reverse))]
        public void SetupReverse() => _reversibleArray = Enumerable.Range(0, 256).ToArray();

        [Benchmark]
        [MemoryRandomization]
        public void Reverse() => Array.Reverse(_reversibleArray);

        [GlobalSetup(Target = nameof(Clear))]
        public void SetupClear() => _clearableArray = new byte[8192];
 
        [Benchmark]
        public void Clear() => Array.Clear(_clearableArray, 0, _clearableArray.Length);

        [GlobalSetup(Target = nameof(IndexOfChar))]
        public void SetupIndexOfChar() => _indexOfCharArray = "This is a test of a reasonably long string to see how IndexOf works".ToCharArray();

        [Benchmark]
        public int IndexOfChar() => Array.IndexOf(_indexOfCharArray, '.');

        [GlobalSetup(Target = nameof(IndexOfShort))]
        public void SetupIndexOfShort() => _indexOfShortArray = "This is a test of a reasonably long string to see how IndexOf works".Select(c => (short)c).ToArray();

        [Benchmark]
        public void IndexOfShort() => Array.IndexOf(_indexOfShortArray, (short)'.');
    }
}