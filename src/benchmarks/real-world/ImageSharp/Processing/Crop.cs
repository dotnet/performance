// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixLabors.ImageSharp.Benchmarks.Processing
{
    public class Crop
    {
        [Benchmark(Description = "ImageSharp Crop")]
        public Size CropImageSharp()
        {
            using var image = new Image<Rgba32>(800, 800);
            image.Mutate(x => x.Crop(100, 100));
            return new Size(image.Width, image.Height);
        }
    }
}
