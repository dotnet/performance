// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace SIMD
{
    [BenchmarkCategory(Categories.Runtime, Categories.SIMD, Categories.JIT)]
    public class ConsoleMandel
    {
        private static void DoNothing(int x, int y, int count) { }

        private static Algorithms.FractalRenderer.Render GetRenderer(Action<int, int, int> draw, int which)
        {
            return Algorithms.FractalRenderer.SelectRender(draw, Abort, IsVector(which), IsDouble(which), IsMulti(which), UsesADT(which), !UseIntTypes(which));
        }

        private static bool Abort() { return false; }

        private static bool UseIntTypes(int num) { return (num & 8) == 0; }

        private static bool IsVector(int num) { return num > 7; }

        private static bool IsDouble(int num) { return (num & 4) != 0; }

        private static bool IsMulti(int num) { return (num & 2) != 0; }

        private static bool UsesADT(int num) { return (num & 1) != 0; }

        public static void XBench(int iters, int which)
        {
            float XC = -1.248f;
            float YC = -.0362f;
            float Range = .001f;
            float xmin = XC - Range;
            float xmax = XC + Range;
            float ymin = YC - Range;
            float ymax = YC + Range;
            float step = Range / 100f;

            Algorithms.FractalRenderer.Render renderer = GetRenderer(DoNothing, which);

            for (int count = 0; count < iters; count++)
            {
                renderer(xmin, xmax, ymin, ymax, step);
            }
        }

        [Benchmark]
        public void ScalarFloatSinglethreadRaw() => XBench(10, 0);

        [Benchmark]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void ScalarFloatSinglethreadADT() => XBench(10, 1);

        [Benchmark]
        public void ScalarDoubleSinglethreadRaw() => XBench(10, 4);

        [Benchmark]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void ScalarDoubleSinglethreadADT() => XBench(10, 5);

        [Benchmark]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void VectorFloatSinglethreadRaw() => XBench(10, 16);

        [Benchmark]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void VectorFloatSinglethreadADT() => XBench(10, 17);

        [Benchmark]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void VectorDoubleSinglethreadRaw() => XBench(10, 20);

        [Benchmark]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void VectorDoubleSinglethreadADT() => XBench(10, 21);
    }
}
