// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Extensions;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD)]
    public class Perf_BitOperations
    {
        private static uint[] input_uint = ValuesGenerator.Array<uint>(1000);
        private static ulong[] input_ulong = ValuesGenerator.Array<ulong>(1000);

        [Benchmark]
        public int LeadingZeroCount_uint()
        {
            int sum = 0;
            uint[] input = input_uint;
            for (int i = 0; i < input.Length; i++)
            {
                sum += BitOperations.LeadingZeroCount(input[i]);
            }
            return sum;
        }

        [Benchmark]
        public int LeadingZeroCount_ulong()
        {
            int sum = 0;
            ulong[] input = input_ulong;
            for (int i = 0; i < input.Length; i++)
            {
                sum += BitOperations.LeadingZeroCount(input[i]);
            }
            return sum;
        }

        [Benchmark]
        public int Log2_uint()
        {
            int sum = 0;
            uint[] input = input_uint;
            for (int i = 0; i < input.Length; i++)
            {
                sum += BitOperations.Log2(input[i]);
            }
            return sum;
        }

        [Benchmark]
        public int Log2_ulong()
        {
            int sum = 0;
            ulong[] input = input_ulong;
            for (int i = 0; i < input.Length; i++)
            {
                sum += BitOperations.Log2(input[i]);
            }
            return sum;
        }

        [Benchmark]
        public int TrailingZeroCount_uint()
        {
            int sum = 0;
            uint[] input = input_uint;
            for (int i = 0; i < input.Length; i++)
            {
                sum += BitOperations.TrailingZeroCount(input[i]);
            }
            return sum;
        }

        [Benchmark]
        public int TrailingZeroCount_ulong()
        {
            int sum = 0;
            ulong[] input = input_ulong;
            for (int i = 0; i < input.Length; i++)
            {
                sum += BitOperations.TrailingZeroCount(input[i]);
            }
            return sum;
        }
    }
}
