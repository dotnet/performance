// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    
    [BenchmarkCategory(Categories.ImageSharp)]
    public class EncodeGif
    {
        // System.Drawing needs this.
        private Stream bmpStream;
        private MemoryStream memoryStream = new MemoryStream();
        private Image<Rgba32> bmpCore;

        // Try to get as close to System.Drawing's output as possible
        private readonly GifEncoder encoder = new GifEncoder
        {
            Quantizer = new WebSafePaletteQuantizer(new QuantizerOptions { Dither = KnownDitherings.Bayer4x4 })
        };

        [Params(TestImages.Bmp.Car, TestImages.Png.Rgb48Bpp)]
        public string TestImage { get; set; }

        [GlobalSetup]
        public void ReadImages()
        {
            if (this.bmpStream == null)
            {
                this.bmpStream = File.OpenRead(Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImage));
                this.bmpCore = Image.Load<Rgba32>(this.bmpStream);
                this.bmpStream.Position = 0;
                this.memoryStream = new MemoryStream();
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            this.bmpStream.Dispose();
            this.bmpStream = null;
            this.bmpCore.Dispose();
        }

        [Benchmark(Description = "ImageSharp Gif")]
        public void GifImageSharp()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            this.bmpCore.SaveAsGif(memoryStream, this.encoder);
        }
    }
}
