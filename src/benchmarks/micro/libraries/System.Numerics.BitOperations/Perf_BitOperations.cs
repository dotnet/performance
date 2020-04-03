// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Runtime.CompilerServices;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD)]
    public class Perf_BitOperations
    {
        private static Random s_random = new Random(5);
        private static uint[] input_uint = GenerateRandomValues<uint>();
        private static ulong[] input_ulong = GenerateRandomValues<ulong>();

        private static T[] GenerateRandomValues<T>() where T : struct
        {
            T[] randomValues = new T[1000];
            for (int i = 0; i < 1000; i++)
            {
                var randomRange = s_random.Next(minValue: 2000 * i, maxValue: 5000 * i);
                randomValues[i] = Unsafe.As<int, T>(ref randomRange);
            }
            return randomValues;
        }

        [Benchmark]
        public int LeadingZeroCount_uint()
        {
            int sum = 0;
            for (int i = 0; i < input_uint.Length; i++)
            {
                sum += BitOperations.LeadingZeroCount(input_uint[i]);
            }
            return sum;
        }

        [Benchmark]
        public int LeadingZeroCount_ulong()
        {
            int sum = 0;
            for (int i = 0; i < input_ulong.Length; i++)
            {
                sum += BitOperations.LeadingZeroCount(input_ulong[i]);
            }
            return sum;
        }

        [Benchmark]
        public int Log2_uint()
        {
            int sum = 0;
            for (int i = 0; i < input_uint.Length; i++)
            {
                sum += BitOperations.Log2(input_uint[i]);
            }
            return sum;
        }

        [Benchmark]
        public int Log2_ulong()
        {
            int sum = 0;
            for (int i = 0; i < input_ulong.Length; i++)
            {
                sum += BitOperations.Log2(input_ulong[i]);
            }
            return sum;
        }

        [Benchmark]
        public int TrailingZeroCount_uint()
        {
            int sum = 0;
            for (int i = 0; i < input_uint.Length; i++)
            {
                sum += BitOperations.TrailingZeroCount(input_uint[i]);
            }
            return sum;
        }

        [Benchmark]
        public int TrailingZeroCount_ulong()
        {
            int sum = 0;
            for (int i = 0; i < input_ulong.Length; i++)
            {
                sum += BitOperations.TrailingZeroCount(input_ulong[i]);
            }
            return sum;
        }
    }
}
