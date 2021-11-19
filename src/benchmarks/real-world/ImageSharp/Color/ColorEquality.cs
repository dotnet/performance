// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;

using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Benchmarks
{
    public class ColorEquality
    {
        [Benchmark(Description = "ImageSharp Color Equals")]
        public bool ColorEqual()
        {
            return new Rgba32(128, 128, 128, 128).Equals(new Rgba32(128, 128, 128, 128));
        }
    }
}
