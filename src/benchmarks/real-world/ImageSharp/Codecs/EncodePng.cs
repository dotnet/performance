// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    
    public class EncodePng
    {
        // System.Drawing needs this.
        private Stream bmpStream;
        private MemoryStream memoryStream;
        private Image<Rgba32> bmpCore;

        [Params(false)]
        public bool LargeImage { get; set; }

        [GlobalSetup]
        public void ReadImages()
        {
            if (this.bmpStream == null)
            {
                string path = Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.LargeImage ? TestImages.Jpeg.Baseline.Jpeg420Exif : TestImages.Bmp.Car);
                this.bmpStream = File.OpenRead(path);
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

        [Benchmark(Description = "ImageSharp Png")]
        public void PngCore()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            var encoder = new PngEncoder { FilterMethod = PngFilterMethod.None };
            this.bmpCore.SaveAsPng(memoryStream, encoder);
        }
    }
}
