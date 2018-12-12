// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

// Performance test for interface call dispatch with two
// possible target classes mixed in varying proportions.

namespace GuardedDevirtualization
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
    public class TwoClassInterface
    {
        interface I
        {
            int F();
        }

        public class B : I
        {
            int I.F() => 33;
        }

        public class D : B, I
        {
            int I.F() => 44;
        }

        [Benchmark(OperationsPerInvoke = TestInput.N)]
        [ArgumentsSource(nameof(GetInput))]
        public long Call(TestInput testInput)
        {
            long sum = 0;
            
            // Note: for now we type the test input as B[] and not I[]
            // so the simple guessing heuristic in the jit has a class
            // to guess for.
            B[] input = testInput.Array;
            for (int i = 0; i < input.Length; i++)
            {
                sum += ((I)input[i]).F();
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


