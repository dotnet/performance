// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Jpeg.Components;

namespace SixLabors.ImageSharp.Benchmarks.Codecs.Jpeg.BlockOperations
{
    [Config(typeof(Config.HwIntrinsics_SSE_AVX))]
    public class Block8x8F_AddInPlace
    {
        [Benchmark]
        public float AddInplace()
        {
            float f = 42F;
            Block8x8F b = default;
            b.AddInPlace(f);
            return f;
        }
    }
}
