// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    [BenchmarkCategory(Categories.ImageSharp)]
    public class DecodeGif
    {
        private byte[] gifBytes;

        private string TestImageFullPath
            => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImage);

        [GlobalSetup]
        public void ReadImages()
        {
            if (this.gifBytes == null)
            {
                this.gifBytes = File.ReadAllBytes(this.TestImageFullPath);
            }
        }

        [Params(TestImages.Gif.Rings)]
        public string TestImage { get; set; }

        [Benchmark(Description = "ImageSharp Gif")]
        public Size GifImageSharp()
        {
            using var memoryStream = new MemoryStream(this.gifBytes);
            using var image = Image.Load<Rgba32>(memoryStream);
            return new Size(image.Width, image.Height);
        }
    }
}
