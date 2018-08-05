// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace System.Numerics.Tests
{
    public class Perf_BigInteger
    {
        public class BigIntegerDataWrapper
        {
            public string Text { get; }
            public byte[] Bytes { get; }
            public BigInteger Value { get; }

            public BigIntegerDataWrapper(string numberString)
            {
                Text = numberString;
                Value = BigInteger.Parse(numberString);
                Bytes = Value.ToByteArray();
            }

            public override string ToString() => Text;
        }

        public static IEnumerable<object> NumberStrings()
        {
            yield return new BigIntegerDataWrapper("123");
            yield return new BigIntegerDataWrapper(int.MinValue.ToString());
            yield return new BigIntegerDataWrapper(string.Concat(Enumerable.Repeat("1234567890", 20)));
        }

        // TODO #18249: Port disabled perf tests from tests\BigInteger\PerformanceTests.cs

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public BigInteger Ctor_ByteArray(BigIntegerDataWrapper numberString) // the argument name is "numberString" to preserve the benchmark ID
            => new BigInteger(numberString.Bytes);

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public byte[] ToByteArray(BigIntegerDataWrapper numberString) 
            => numberString.Value.ToByteArray();

        [Benchmark]
        [ArgumentsSource(nameof(NumberStrings))]
        public BigInteger Parse(BigIntegerDataWrapper numberString) 
            => BigInteger.Parse(numberString.Text);
    }
}