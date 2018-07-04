// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Test.Cryptography;

namespace System.Security.Cryptography.Primitives.Tests.Performance
{
    public class Perf_FixedTimeEquals
    {
        private const int IterationCountFor256Bit = 300000;

        byte[] baseValue, errorVector;

        [GlobalSetup(Target = nameof(FixedTimeEquals_256Bit_Equal))]
        public void Setup_Equal()
            => Setup(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "0000000000000000000000000000000000000000000000000000000000000000");

        [Benchmark]
        public void FixedTimeEquals_256Bit_Equal() => MeasureFixedTimeEquals(baseValue, errorVector);

        
        [GlobalSetup(Target = nameof(FixedTimeEquals_256Bit_LastBitDifferent))]
        public void Setup_LastBitDifferent()
            => Setup(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "0000000000000000000000000000000000000000000000000000000000000001");

        [Benchmark]
        public void FixedTimeEquals_256Bit_LastBitDifferent() => MeasureFixedTimeEquals(baseValue, errorVector);

        
        [GlobalSetup(Target = nameof(FixedTimeEquals_256Bit_FirstBitDifferent))]
        public void Setup_FirstBitDifferent()
            => Setup(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "8000000000000000000000000000000000000000000000000000000000000000");
        
        [Benchmark]
        public void FixedTimeEquals_256Bit_FirstBitDifferent() => MeasureFixedTimeEquals(baseValue, errorVector);

        [GlobalSetup(Target = nameof(FixedTimeEquals_256Bit_CascadingErrors))]
        public void Setup_CascadingErrors()
            => Setup(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "0102040810204080112244880000000000000000000000000000000000000000");
        
        [Benchmark]
        public void FixedTimeEquals_256Bit_CascadingErrors() => MeasureFixedTimeEquals(baseValue, errorVector);

        [GlobalSetup(Target = nameof(FixedTimeEquals_256Bit_AllBitsDifferent))]
        public void Setup_AllBitsDifferent()
            => Setup(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
        
        [Benchmark]
        public void FixedTimeEquals_256Bit_AllBitsDifferent() => MeasureFixedTimeEquals(baseValue, errorVector);

        [GlobalSetup(Target = nameof(FixedTimeEquals_256Bit_VersusZero))]
        public void Setup_VersusZero()
            => Setup(
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336",
                "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336");
        
        [Benchmark]
        public void FixedTimeEquals_256Bit_VersusZero() => MeasureFixedTimeEquals(baseValue, errorVector);
        
        [GlobalSetup(Target = nameof(FixedTimeEquals_256Bit_SameReference))]
        public void Setup_SameReference()
            => baseValue = "741202531e19d673ad7fff334594549e7c81a285dd02865ddd12530612a96336".HexToByteArray();

        [Benchmark]
        public void FixedTimeEquals_256Bit_SameReference() => MeasureFixedTimeEquals(baseValue, baseValue);

        private void Setup(string baseValueHex, string errorVectorHex)
        {
            if (errorVectorHex.Length != baseValueHex.Length)
            {
                throw new InvalidOperationException();
            }

            byte[] a = baseValueHex.HexToByteArray();
            byte[] b = errorVectorHex.HexToByteArray();

            for (int i = 0; i < a.Length; i++)
            {
                b[i] ^= a[i];
            }

            baseValue = a;
            errorVector = b;
        }

        // The important statistics from these perf runs aren't the mean, but the t-test for
        // every set of the same length being the same as when it was equal.
        private static void MeasureFixedTimeEquals(byte[] a, byte[] b)
        {
            Span<byte> left = a;
            Span<byte> right = b;

            for (int i = 0; i < IterationCountFor256Bit; i++)
            {
                CryptographicOperations.FixedTimeEquals(left, right);
            }
        }
    }
}
