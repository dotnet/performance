// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MicroBenchmarks;
using System.Runtime.CompilerServices;

namespace IfStatements
{
    [BenchmarkCategory(Categories.Runtime)]
    public unsafe class IfStatements
    {
        private const int Iterations = 10000;
        private const int MaxArgsPassed = 4; // Num of args in Consume

        private static int[] inputs;

        private static int s_seed;

        static void InitRand() {
            s_seed = 7774755;
        }

        static int Rand(ref int seed) {
            s_seed = (s_seed * 77 + 13218009) % 3687091;
            return seed;
        }

        public IfStatements()
        {
            inputs = new int[Iterations + MaxArgsPassed];
            for (int i = 0; i < inputs.Length; i++) {
                inputs[i] = Rand(ref s_seed) - 1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Consume(int op1, int op2, int op3, int op4) {
            return;
        }

        [Benchmark]
        public void Single() {
            for (int i = 0; i < Iterations; i++) {
                SingleInner(inputs[i]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SingleInner(int op1) {
            if (op1 % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, 0, 0, 0);
        }

        [Benchmark]
        public void And() {
            for (int i = 0; i < Iterations; i++) {
                AndInner(inputs[i], inputs[i+1]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AndInner(int op1, int op2) {
            if (op1 % 2 == 0 && op2 % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, 0, 0);
        }

        [Benchmark]
        public void AndAnd() {
            for (int i = 0; i < Iterations; i++) {
                AndAndInner(inputs[i], inputs[i+1], inputs[i+2]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AndAndInner(int op1, int op2, int op3) {
            if (op1 % 2 == 0 && op2 % 2 == 0 && op3 % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, op3, 0);
        }

        [Benchmark]
        public void AndAndAnd() {
            for (int i = 0; i < Iterations; i++) {
                AndAndAndInner(inputs[i], inputs[i+1], inputs[i+2], inputs[i+3]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AndAndAndInner(int op1, int op2, int op3, int op4) {
            if (op1 % 2 == 0 && op2 % 2 == 0 && op3 % 2 == 0 && op4 % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, op3, op4);
        }

        [Benchmark]
        public void Or() {
            for (int i = 0; i < Iterations; i++) {
                OrInner(inputs[i], inputs[i+1]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OrInner(int op1, int op2) {
            if (op1 % 2 == 0 || op2 % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, 0, 0);
        }

        [Benchmark]
        public void OrOr() {
            for (int i = 0; i < Iterations; i++) {
                OrOrInner(inputs[i], inputs[i+1], inputs[i+2]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OrOrInner(int op1, int op2, int op3) {
            if (op1 % 2 == 0 || op2 % 2 == 0 || op3 % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, op3, 0);
        }

        [Benchmark]
        public void AndOr() {
            for (int i = 0; i < Iterations; i++) {
                AndOrInner(inputs[i], inputs[i+1], inputs[i+2]);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AndOrInner(int op1, int op2, int op3) {
            if (op1 % 2 == 0 && op2 % 2 == 0 || op3 % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, op3, 0);
        }

        [Benchmark]
        public void SingleArray() {
            for (int i = 0; i < Iterations; i++) {
                SingleArrayInner(i);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SingleArrayInner(int op1) {
            if (inputs[op1] % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, 0, 0, 0);
        }

        [Benchmark]
        public void AndArray() {
            for (int i = 0; i < Iterations; i++) {
                AndArrayInner(i, i+1);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AndArrayInner(int op1, int op2) {
            if (inputs[op1] % 2 == 0 && inputs[op2] % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, 0, 0);
        }

        [Benchmark]
        public void OrArray() {
            for (int i = 0; i < Iterations; i++) {
                OrArrayInner(i, i+1);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OrArrayInner(int op1, int op2) {
            if (inputs[op1] % 2 == 0 || inputs[op2] % 2 == 0) {
                op1 = 5;
            }
            Consume(op1, op2, 0, 0);
        }
    }
}
