// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    [Config(typeof(Config.ShortMultiFramework))]
    public class EncodeTga
    {
        private Image<Rgba32> tga;

        private string TestImageFullPath
            => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImage);

        [Params(TestImages.Tga.Bit24BottomLeft)]
        public string TestImage { get; set; }

        [GlobalSetup]
        public void ReadImages()
        {
            if (this.tga == null)
            {
                this.tga = Image.Load<Rgba32>(this.TestImageFullPath);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            this.tga.Dispose();
            this.tga = null;
        }

        [Benchmark(Description = "ImageSharp Tga")]
        public void ImageSharpTga()
        {
            using var memoryStream = new MemoryStream();
            this.tga.SaveAsTga(memoryStream);
        }
    }
}
