// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public class Perf_Array
    {
        private static int[] s_arr;
        private static Array s_arr1;
        private static Array s_arr2;
        private static Array s_arr3;
        private static Array _destinationArray;
        private byte[][] _byteArrays;  

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
        public void SetupArrayAssign1D() => s_arr1 = Array.CreateInstance(typeof(int), s_DIM_1);

        [Benchmark]
        public void ArrayAssign1D()
        {
            for (int j = 0; j < s_DIM_1; j++)
            {
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
                s_arr1.SetValue(j, j);
            }
        }

        [GlobalSetup(Target = nameof(ArrayAssign2D))]
        public void SetupArrayAssign2D() => s_arr2 = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);

        [Benchmark]
        public void ArrayAssign2D()
        {
            for (int j = 0; j < s_DIM_2; j++)
            {
                for (int k = 0; k < s_DIM_2; k++)
                {
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                    s_arr2.SetValue(j + k, j, k);
                }
            }
        }

        [GlobalSetup(Target = nameof(ArrayAssign3D))]
        public void SetupArrayAssign3D() => s_arr3 = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);

        [Benchmark]
        public void ArrayAssign3D()
        {
            for (int j = 0; j < s_DIM_3; j++)
            {
                for (int k = 0; k < s_DIM_3; k++)
                {
                    for (int l = 0; l < s_DIM_3; l++)
                    {
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                        s_arr3.SetValue(j + k + l, j, k, l);
                    }
                }
            }
        }

        [GlobalSetup(Target = nameof(ArrayRetrieve1D))]
        public void SetupArrayRetrieve1D()
        {
            s_arr1 = Array.CreateInstance(typeof(int), s_DIM_1);

            for (int i = 0; i < s_DIM_1; i++)
                s_arr1.SetValue(i, i);
        }

        [Benchmark]
        public int ArrayRetrieve1D()
        {
            int value = default;

            for (int j = 0; j < s_DIM_1; j++)
            {
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
                value = (int) s_arr1.GetValue(j);
            }

            return value;
        }

        [GlobalSetup(Target = nameof(ArrayRetrieve2D))]
        public void SetupArrayRetrieve2D()
        {
            s_arr2 = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);

            for (int i = 0; i < s_DIM_2; i++)
            {
                for (int j = 0; j < s_DIM_2; j++)
                    s_arr2.SetValue(i + j, i, j);
            }
        }

        [Benchmark]
        public int ArrayRetrieve2D()
        {
            int value = default;

            for (int j = 0; j < s_DIM_2; j++)
            {
                for (int k = 0; k < s_DIM_2; k++)
                {
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                    value = (int) s_arr2.GetValue(j, k);
                }
            }

            return value;
        }

        [GlobalSetup(Target = nameof(ArrayRetrieve3D))]
        public void SetupArrayRetrieve3D()
        {
            s_arr3 = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);

            for (int i = 0; i < s_DIM_3; i++)
            {
                for (int j = 0; j < s_DIM_3; j++)
                {
                    for (int k = 0; k < s_DIM_3; k++)
                        s_arr3.SetValue(i + j + k, i, j, k);
                }
            }
        }

        [Benchmark]
        public int ArrayRetrieve3D()
        {
            int value = default;

            for (int j = 0; j < s_DIM_3; j++)
            {
                for (int k = 0; k < s_DIM_3; k++)
                {
                    for (int l = 0; l < s_DIM_3; l++)
                    {
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                        value = (int) s_arr3.GetValue(j, k, l);
                    }
                }
            }

            return value;
        }

        [GlobalSetup(Target = nameof(ArrayCopy2D))]
        public void SetupArrayCopy2D()
        {
            _destinationArray = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);
            s_arr2 = Array.CreateInstance(typeof(int), s_DIM_2, s_DIM_2);

            for (int i = 0; i < s_DIM_2; i++)
            {
                for (int j = 0; j < s_DIM_2; j++)
                    s_arr2.SetValue(i + j, i, j);
            }
        }

        [Benchmark]
        public void ArrayCopy2D() => Array.Copy(s_arr2, _destinationArray, s_DIM_2 * s_DIM_2);

        [GlobalSetup(Target = nameof(ArrayCopy3D))]
        public void SetupArrayCopy3D()
        {
            _destinationArray = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);
            s_arr3 = Array.CreateInstance(typeof(int), s_DIM_3, s_DIM_3, s_DIM_3);

            for (int i = 0; i < s_DIM_3; i++)
            {
                for (int j = 0; j < s_DIM_3; j++)
                {
                    for (int k = 0; k < s_DIM_3; k++)
                    {
                        s_arr3.SetValue(i + j + k, i, j, k);
                    }
                }
            }
        }

        [Benchmark]
        public void ArrayCopy3D() => Array.Copy(s_arr3, _destinationArray, s_DIM_3 * s_DIM_3 * s_DIM_3);

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
    }
}