// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

// Performance test for virtual call dispatch with two
// possible target classes mixed in varying proportions.

namespace GuardedDevirtualization
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
    public class TwoClassVirtual
    {
        public class B
        {
            public virtual int F() => 33;
        }

        public class D : B
        {
            public override int F() => 44;
        }

        [Benchmark(OperationsPerInvoke = TestInput.N)]
        [ArgumentsSource(nameof(GetInput))]
        public long Call(TestInput testInput)
        {
            long sum = 0;
            B[] input = testInput.Array;
            for (int i = 0; i < input.Length; i++)
            {
                sum += input[i].F();
            }

            return sum;
        }

        public static IEnumerable<TestInput> GetInput()
        {
            for (double pB = 0; pB <= 1.0; pB += 0.1)
            {
                yield return new TestInput(pB);
            }
        }

        public class TestInput
        {
            public const int N = 1000;
            
            public B[] Array;
            private double _pB;

            public TestInput(double pB)
            {
                _pB = pB;
                Array = ValuesGenerator.Array<double>(N).Select(p => p > _pB ? new D() : new B()).ToArray();
            }

            public override string ToString() => $"pB = {_pB:F2}";
        }
    }
}