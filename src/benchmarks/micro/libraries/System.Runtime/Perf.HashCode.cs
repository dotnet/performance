// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_HashCode
    {
        [Benchmark]
        public int Add()
        { 
            var hc = new HashCode();
            for (int j = 0; j < 100; j++)
            {
                hc.Add(j); hc.Add(j); hc.Add(j);
                hc.Add(j); hc.Add(j); hc.Add(j);
                hc.Add(j); hc.Add(j); hc.Add(j);
            }
            return hc.ToHashCode();
        }

        [Benchmark]
        public int Combine_1()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            { 
                result = HashCode.Combine(
                    i + result);
            }
            return result;
        }

        [Benchmark]
        public int Combine_2()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = HashCode.Combine(
                    i + result,
                    i + result);
            }
            return result;
        }

        [Benchmark]
        public int Combine_3()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = HashCode.Combine(
                    i + result,
                    i + result,
                    i + result);
            }
            return result;
        }

        [Benchmark]
        public int Combine_4()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = HashCode.Combine(
                    i + result,
                    i + result,
                    i + result,
                    i + result);
            }
            return result;
        }

        [Benchmark]
        public int Combine_5()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = HashCode.Combine(
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result);
            }
            return result;
        }

        [Benchmark]
        public int Combine_6()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = HashCode.Combine(
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result);
            }
            return result;
        }

        [Benchmark]
        public int Combine_7()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = HashCode.Combine(
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result);
            }
            return result;
        }

        [Benchmark]
        public int Combine_8()
        {
            int result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = HashCode.Combine(
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result,
                    i + result);
            }
            return result;
        }
    }
}
