// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MicroBenchmarks;
using System.Runtime.CompilerServices;

namespace IfLoops
{
    [BenchmarkCategory(Categories.Runtime)]
    public unsafe class IfLoops
    {
        private const int Iterations = 10000;
        private const int SpillBuffer = 1;

        private static int[] inputs;
        private static int[] inputs_sequential;
        private static int[] inputs_sequential_null;
        private static int[] inputs_zeros;

        private static int s_seed;

        static void InitRand() {
            s_seed = 7774755;
        }

        static int Rand(ref int seed) {
            s_seed = (s_seed * 77 + 13218009) % 3687091;
            return seed;
        }

        public IfLoops()
        {
            inputs = new int[Iterations + SpillBuffer];
            inputs_sequential = new int[Iterations + SpillBuffer];
            inputs_sequential_null = new int[Iterations + SpillBuffer];
            inputs_zeros = new int[Iterations + SpillBuffer];
            for (int i = 0; i < inputs.Length; i++) {
                inputs[i] = Rand(ref s_seed) - 1;
                inputs_sequential[i] = i;
                inputs_sequential_null[i] = (i < Iterations) ? i + 1 : 0;
                inputs_zeros[i] = 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Consume(int op1, int op2, int op3, int op4) {
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SingleInner(int op1, int mod) {
            if (op1 % mod == 0) {
                op1 = 5;
            }
            Consume(op1, 0, 0, 0);
            return op1;
        }


        /* Inner branch will be taken randomly, approximately 1 in 2 times. Loop size is known. */
        [Benchmark]
        public void Single_Random2() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs[i], 2);
            }
        }

        /* Inner branch will be taken randomly, approximately 1 in 3 times. Loop size is known. */
        [Benchmark]
        public void Single_Random3() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs[i], 3);
            }
        }

        /* Inner branch will be taken randomly, approximately 1 in 4 times. Loop size is known. */
        [Benchmark]
        public void Single_Random4() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs[i], 4);
            }
        }

        /* Inner branch will be taken randomly, approximately 1 in 5 times. Loop size is known. */
        [Benchmark]
        public void Single_Random5() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs[i], 5);
            }
        }

        /* Inner branch will be taken randomly, approximately 1 in 2 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Random2UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs[i], 2);
            }
        }

        /* Inner branch will be taken randomly, approximately 1 in 3 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Random3UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs[i], 3);
            }
        }

        /* Inner branch will be taken randomly, approximately 1 in 4 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Random4UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs[i], 4);
            }
        }

        /* Inner branch will be taken randomly, approximately 1 in 5 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Random5UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs[i], 5);
            }
        }


        /* Inner branch will be taken in a pattern, every 1 in 2 times. Loop size is known. */
        [Benchmark]
        public void Single_Seq2() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs_sequential[i], 2);
            }
        }

        /* Inner branch will be taken in a pattern, every 1 in 3 times. Loop size is known. */
        [Benchmark]
        public void Single_Seq3() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs_sequential[i], 3);
            }
        }

        /* Inner branch will be taken in a pattern, every 1 in 4 times. Loop size is known. */
        [Benchmark]
        public void Single_Seq4() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs_sequential[i], 4);
            }
        }

        /* Inner branch will be taken in a pattern, every 1 in 5 times. Loop size is known. */
        [Benchmark]
        public void Single_Seq5() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs_sequential[i], 5);
            }
        }


        /* Inner branch will be taken in a pattern, every 1 in 2 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Seq2UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs_sequential[i], 2);
            }
        }

        /* Inner branch will be taken in a pattern, every 1 in 3 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Seq3UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs_sequential[i], 3);
            }
        }

        /* Inner branch will be taken in a pattern, every 1 in 4 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Seq4UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs_sequential[i], 4);
            }
        }

        /* Inner branch will be taken in a pattern, every 1 in 5 times. Loop size is unknown. */
        [Benchmark]
        public void Single_Seq5UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs_sequential[i], 5);
            }
        }

        /* Inner branch will be taken always.  Loop size is known. */
        [Benchmark]
        public void Single_Always() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs_zeros[i], 1);
            }
        }

        /* Inner branch will be taken always.  Loop size is unknown. */
        [Benchmark]
        public void Single_AlwaysUnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                SingleInner(inputs_zeros[i], 1);
            }
        }

        /* Benchmarks repeated as above, but with two conditions tested. */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AndInner(int op1, int op2, int mod) {
            if (op1 % mod == 0 && op2 % mod == 0) {
                op1 = 5;
            }
            Consume(op1, op2, 0, 0);
        }

        [Benchmark]
        public void And_Random2() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs[i], inputs[i+1], 2);
            }
        }

        [Benchmark]
        public void And_Random3() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs[i], inputs[i+1], 3);
            }
        }

        [Benchmark]
        public void And_Random4() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs[i], inputs[i+1], 4);
            }
        }

        [Benchmark]
        public void And_Random5() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs[i], inputs[i+1], 5);
            }
        }


        [Benchmark]
        public void And_Random2UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs[i], inputs[i+1], 2);
            }
        }

        [Benchmark]
        public void And_Random3UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs[i], inputs[i+1], 3);
            }
        }

        [Benchmark]
        public void And_Random4UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs[i], inputs[i+1], 4);
            }
        }

        [Benchmark]
        public void And_Random5UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs[i], inputs[i+1], 5);
            }
        }

        [Benchmark]
        public void And_Seq2() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 2);
            }
        }

        [Benchmark]
        public void And_Seq3() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 3);
            }
        }

        [Benchmark]
        public void And_Seq4() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 4);
            }
        }

        [Benchmark]
        public void And_Seq5() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 5);
            }
        }

        [Benchmark]
        public void And_Seq2UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 2);
            }
        }

        [Benchmark]
        public void And_Seq3UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 3);
            }
        }

        [Benchmark]
        public void And_Seq4UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 4);
            }
        }

        [Benchmark]
        public void And_Seq5UnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs_sequential[i], inputs_sequential[i+1], 5);
            }
        }

        [Benchmark]
        public void And_Always() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs_zeros[i], inputs_zeros[i+1], 1);
            }
        }

        [Benchmark]
        public void And_AlwaysUnknownSize() {
            for (int i = 0; inputs_sequential_null[i] != 0; i++) {
                AndInner(inputs_zeros[i], inputs_zeros[i+1], 1);
            }
        }
    }
}
