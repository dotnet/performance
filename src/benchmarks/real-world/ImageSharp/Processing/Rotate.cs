// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixLabors.ImageSharp.Benchmarks.Processing
{
    [BenchmarkCategory(Categories.ImageSharp)]
    public class Rotate
    {
        [Benchmark]
        public Size DoRotate()
        {
            using var image = new Image<Rgba32>(Configuration.Default, 400, 400, Color.BlanchedAlmond);
            image.Mutate(x => x.Rotate(37.5F));

            return image.Size();
        }
    }
}