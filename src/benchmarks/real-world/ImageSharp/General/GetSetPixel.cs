// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Benchmarks
{
    public class GetSetPixel
    {
        [Benchmark(Description = "ImageSharp GetSet pixel")]
        public Rgba32 GetSetImageSharp()
        {
            using var image = new Image<Rgba32>(400, 400);
            image[200, 200] = Color.White;
            return image[200, 200];
        }
    }
}
