// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Numerics.Tests
{
    public class PerformanceTests
    {
        public IEnumerable<object> ValuesSameSize()
        {
            yield return new BigIntegers(new[] { 16, 16 });
            yield return new BigIntegers(new[] { 1024, 1024 });
            yield return new BigIntegers(new[] { 65536, 65536 });
        }

        [Benchmark]
        [ArgumentsSource(nameof(ValuesSameSize))]
        public BigInteger Add(BigIntegers arguments)
            => BigInteger.Add(arguments.Left, arguments.Right);

        [Benchmark]
        [ArgumentsSource(nameof(ValuesSameSize))]
        public BigInteger Subtract(BigIntegers arguments)
            => BigInteger.Subtract(arguments.Left, arguments.Right);

        [Benchmark]
        [ArgumentsSource(nameof(ValuesSameSize))]
        public BigInteger Multiply(BigIntegers arguments)
            => BigInteger.Multiply(arguments.Left, arguments.Right);

        [Benchmark]
        [ArgumentsSource(nameof(ValuesSameSize))]
        public BigInteger GreatestCommonDivisor(BigIntegers arguments)
            => BigInteger.GreatestCommonDivisor(arguments.Left, arguments.Right);

        public IEnumerable<object> ValuesHalfSize()
        {
            yield return new BigIntegers(new[] { 16, 16 / 2 });
            yield return new BigIntegers(new[] { 1024, 1024 / 2 });
            yield return new BigIntegers(new[] { 65536, 65536 / 2 });
        }

        [Benchmark]
        [ArgumentsSource(nameof(ValuesHalfSize))]
        public BigInteger Divide(BigIntegers arguments)
            => BigInteger.Divide(arguments.Left, arguments.Right);

        [Benchmark]
        [ArgumentsSource(nameof(ValuesHalfSize))]
        public BigInteger Remainder(BigIntegers arguments)
            => BigInteger.Remainder(arguments.Left, arguments.Right);

        public IEnumerable<object> ModPowValues()
        {
            yield return new BigIntegers(new[] { 16, 16, 16 });
            yield return new BigIntegers(new[] { 1024, 1024, 64 });
            yield return new BigIntegers(new[] { 16384, 16384, 64 });
        }

        [Benchmark]
        [ArgumentsSource(nameof(ModPowValues))]
        public BigInteger ModPow(BigIntegers arguments)
            => BigInteger.ModPow(arguments.Left, arguments.Right, arguments.Other);

        public class BigIntegers
        {
            private readonly int[] _bitCounts;
            
            public BigInteger Left { get; }
            public BigInteger Right { get; }
            public BigInteger Other { get; }

            public BigIntegers(int[] bitCounts)
            {
                _bitCounts = bitCounts;
                var values = GenerateBigIntegers(bitCounts);

                Left = values[0];
                Right = values[1];
                Other = values.Length == 3 ? values[2] : BigInteger.Zero;
            }

            public override string ToString() => $"{string.Join(",", _bitCounts)} bits";

            private BigInteger[] GenerateBigIntegers(int[] bitCounts)
            {
                Random random = new Random(1138); // we always use the same seed to have repeatable results!

                BigInteger[] result = new BigInteger[bitCounts.Length];

                for (int i = 0; i < bitCounts.Length; i++)
                    result[i] = CreateRandomInteger(random, bitCounts[i]);

                return result;
            }

            private static BigInteger CreateRandomInteger(Random random, int bits)
            {
                byte[] value = new byte[(bits + 8) / 8];
                BigInteger result = BigInteger.Zero;

                while (result.IsZero)
                {
                    random.NextBytes(value);

                    // ensure actual bit count (remaining bits not set)
                    // ensure positive value (highest-order bit not set)
                    value[value.Length - 1] &= (byte) (0xFF >> 8 - bits % 8);

                    result = new BigInteger(value);
                }

                return result;
            }
        }
    }
}