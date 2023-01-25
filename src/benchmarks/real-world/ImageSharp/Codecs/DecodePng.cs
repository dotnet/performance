// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    
    [BenchmarkCategory(Categories.ImageSharp)]
    public class DecodePng
    {
        private byte[] pngBytes;

        private string TestImageFullPath
            => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImage);

        [Params(TestImages.Png.Splash)]
        public string TestImage { get; set; }

        [GlobalSetup]
        public void ReadImages()
        {
            if (this.pngBytes == null)
            {
                this.pngBytes = File.ReadAllBytes(this.TestImageFullPath);
            }
        }

        [Benchmark(Description = "ImageSharp Png")]
        public Size PngImageSharp()
        {
            using var memoryStream = new MemoryStream(this.pngBytes);
            using var image = Image.Load<Rgba32>(memoryStream);
            return image.Size();
        }
    }
}
