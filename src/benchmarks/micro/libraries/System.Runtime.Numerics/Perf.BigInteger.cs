// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_BigInteger
    {
        public IEnumerable<object> NumberStrings()
        {
            yield return new BigIntegerData("123");
            yield return new BigIntegerData(int.MinValue.ToString());
            yield return new BigIntegerData(string.Concat(Enumerable.Repeat("1234567890", 20)));
        }

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public BigInteger Ctor_ByteArray(BigIntegerData numberString) // the argument name is "numberString" to preserve the benchmark ID
            => new BigInteger(numberString.Bytes);

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public byte[] ToByteArray(BigIntegerData numberString)
            => numberString.Value.ToByteArray();

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public BigInteger Parse(BigIntegerData numberString)
            => BigInteger.Parse(numberString.Text);

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public string ToStringX(BigIntegerData numberString)
            => numberString.Value.ToString("X");

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public string ToStringD(BigIntegerData numberString)
            => numberString.Value.ToString("D");

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
        public BigInteger GreatestCommonDivisor(BigIntegers arguments)
            => BigInteger.GreatestCommonDivisor(arguments.Left, arguments.Right);

        public IEnumerable<object> ValuesHalfSize()
        {
            yield return new BigIntegers(new[] { 16, 16 / 2 });
            yield return new BigIntegers(new[] { 1024, 1024 / 2 });
            yield return new BigIntegers(new[] { 65536, 65536 / 2 });
        }

        public IEnumerable<object> ValuesSameOrHalfSize()
        {
            foreach (var item in ValuesSameSize()) yield return item;
            foreach (var item in ValuesHalfSize()) yield return item;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ValuesSameOrHalfSize))]
        [MemoryRandomization]
        public BigInteger Multiply(BigIntegers arguments)
            => BigInteger.Multiply(arguments.Left, arguments.Right);

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

        public IEnumerable<object> EqualsValues()
        {
            Random rnd = new Random(123456);

            foreach (int byteCount in new[] { 67, 259 })
            {
                byte[] bytes = new byte[byteCount];
                int lastByte = bytes.Length - 1;

                do
                {
                    rnd.NextBytes(bytes);
                } while (bytes[lastByte] is not (> 0 and < 128));

                BigInteger b1 = new(bytes);
                yield return new BigIntegers(b1, new BigInteger(bytes), $"{byteCount} bytes, Same");

                byte copy = bytes[lastByte];
                bytes[lastByte] = (byte)(~bytes[lastByte] & 0x7F);
                yield return new BigIntegers(b1, new BigInteger(bytes), $"{byteCount} bytes, DiffLastByte");
                bytes[lastByte] = copy;

                copy = bytes[0];
                bytes[0] = (byte)(~bytes[0] & 0x7F);
                yield return new BigIntegers(b1, new BigInteger(bytes), $"{byteCount} bytes, DiffFirstByte");
                bytes[0] = copy;

                copy = bytes[byteCount / 2];
                bytes[byteCount / 2] = (byte)(~bytes[byteCount / 2] & 0x7F);
                yield return new BigIntegers(b1, new BigInteger(bytes), $"{byteCount} bytes, DiffMiddleByte");
                bytes[byteCount / 2] = copy;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(EqualsValues))]
        public bool Equals(BigIntegers arguments)
            => arguments.Left == arguments.Right;

        public class BigIntegerData
        {
            public string Text { get; }
            public byte[] Bytes { get; }
            public BigInteger Value { get; }

            public BigIntegerData(string numberString)
            {
                Text = numberString;
                Value = BigInteger.Parse(numberString);
                Bytes = Value.ToByteArray();
            }

            public override string ToString() => Text;
        }

        public class BigIntegers
        {
            private readonly int[] _bitCounts;
            private readonly string _description;

            public BigInteger Left { get; }
            public BigInteger Right { get; }
            public BigInteger Other { get; }

            public BigIntegers(int[] bitCounts)
            {
                _bitCounts = bitCounts;
                _description = $"{string.Join(",", _bitCounts)} bits";
                var values = GenerateBigIntegers(bitCounts);

                Left = values[0];
                Right = values[1];
                Other = values.Length == 3 ? values[2] : BigInteger.Zero;
            }

            public BigIntegers(BigInteger left, BigInteger right, string description)
            {
                Left = left;
                Right = right;
                _description = description;
            }

            public override string ToString() => _description;

            private static BigInteger[] GenerateBigIntegers(int[] bitCounts)
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
                    value[value.Length - 1] &= (byte)(0xFF >> 8 - bits % 8);

                    result = new BigInteger(value);
                }

                return result;
            }
        }
    }
}