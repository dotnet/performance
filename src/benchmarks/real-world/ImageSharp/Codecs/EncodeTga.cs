// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    [BenchmarkCategory(Categories.ImageSharp)]
    public class EncodeTga
    {
        private MemoryStream memoryStream;
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
                this.memoryStream = new MemoryStream();
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
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            this.tga.SaveAsTga(memoryStream);
        }
    }
}
