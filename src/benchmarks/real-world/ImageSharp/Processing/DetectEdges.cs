// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks
{
    [BenchmarkCategory(Categories.ImageSharp)]
    public class DetectEdges
    {
        private Image<Rgba32> image;

        [GlobalSetup]
        public void ReadImage()
        {
            if (this.image == null)
            {
                this.image = Image.Load<Rgba32>(File.OpenRead(Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, TestImages.Bmp.Car)));
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            this.image.Dispose();
            this.image = null;
        }

        [Benchmark(Description = "ImageSharp DetectEdges")]
        public void ImageProcessorCoreDetectEdges()
        {
            this.image.Mutate(x => x.DetectEdges(KnownEdgeDetectorKernels.Laplacian5x5));
        }
    }
}
