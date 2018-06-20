// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Benchmarks;

namespace PerfLabTests
{
    public enum Color
    {
        Black,
        White,
        Red,
        Brown,
        Yellow,
        Purple,
        Orange
    }

    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class EnumPerf
    {
        public static int InnerIterationCount = 300000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        
        public static Color blackColor;
        public static object blackObject;
        
        [Benchmark]
        [Arguments(Color.Red)]
        public void EnumCompareTo(Color color)
        {
            Color white = Color.White;

            for (int i = 0; i < InnerIterationCount; i++)
                color.CompareTo(white);
        }

        [GlobalSetup(Target = nameof(ObjectGetType))]
        public void SetupObjectGetType() => blackColor = Color.Black;

        // [Benchmark] disabled for now -> is optimized by JIT to an empty loop, #42
        public Type ObjectGetType()
        {
            Type tmp = null;
        
            for (int i = 0; i < InnerIterationCount; i++)
                tmp = blackColor.GetType();
        
            return tmp;
        }

        [GlobalSetup(Target = nameof(ObjectGetTypeNoBoxing))]
        public void SetupObjectGetTypeNoBoxing() => blackObject = Color.Black;

        [Benchmark]
        public Type ObjectGetTypeNoBoxing()
        {
            Type tmp = null;

            for (int i = 0; i < InnerIterationCount; i++)
                tmp = blackObject.GetType();

            return tmp;
        }

        [Benchmark]
        public bool EnumEquals()
        {
            Color black = Color.Black;
            Color white = Color.White;
            bool tmp = false;

            for (int i = 0; i < InnerIterationCount; i++)
                tmp = black.Equals(white);

            return tmp;
        }
    }

}
