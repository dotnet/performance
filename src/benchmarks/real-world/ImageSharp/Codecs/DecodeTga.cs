// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    public class DecodeTga
    {
        private string TestImageFullPath => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImage);

        private byte[] data;

        [Params(TestImages.Tga.Bit24BottomLeft)]
        public string TestImage { get; set; }

        [GlobalSetup]
        public void SetupData()
            => this.data = File.ReadAllBytes(this.TestImageFullPath);

        [Benchmark(Description = "ImageSharp Tga")]
        public int TgaImageSharp()
        {
            using var image = Image.Load<Bgr24>(this.data, new TgaDecoder());
            return image.Width;
        }
    }
}
