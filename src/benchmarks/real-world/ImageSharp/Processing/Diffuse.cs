// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixLabors.ImageSharp.Benchmarks.Processing
{
    public class Diffuse
    {
        [Benchmark]
        public Size DoDiffuse()
        {
            using var image = new Image<Rgba32>(Configuration.Default, 800, 800, Color.BlanchedAlmond);
            image.Mutate(x => x.Dither(KnownDitherings.FloydSteinberg));

            return image.Size();
        }

        [Benchmark]
        public Size DoDither()
        {
            using var image = new Image<Rgba32>(Configuration.Default, 800, 800, Color.BlanchedAlmond);
            image.Mutate(x => x.Dither());

            return image.Size();
        }
    }
}