// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    /// <summary>
    /// Benchmarks saving png files using different quantizers.
    /// System.Drawing cannot save indexed png files so we cannot compare.
    /// </summary>
    [BenchmarkCategory(Categories.ImageSharp)]
    public class EncodeIndexedPng
    {
        // System.Drawing needs this.
        private Stream bmpStream;
        private MemoryStream memoryStream;
        private Image<Rgba32> bmpCore;

        [GlobalSetup]
        public void ReadImages()
        {
            if (this.bmpStream == null)
            {
                this.bmpStream = File.OpenRead(Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, TestImages.Bmp.Car));
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

        [Benchmark(Description = "ImageSharp Octree Png")]
        public void PngCoreOctree()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            var options = new PngEncoder { Quantizer = KnownQuantizers.Octree };
            this.bmpCore.SaveAsPng(memoryStream, options);
        }

        [Benchmark(Description = "ImageSharp Octree NoDither Png")]
        public void PngCoreOctreeNoDither()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            var options = new PngEncoder { Quantizer = new OctreeQuantizer(new QuantizerOptions { Dither = null }) };
            this.bmpCore.SaveAsPng(memoryStream, options);
        }

        [Benchmark(Description = "ImageSharp Palette Png")]
        public void PngCorePalette()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            var options = new PngEncoder { Quantizer = KnownQuantizers.WebSafe };
            this.bmpCore.SaveAsPng(memoryStream, options);
        }

        [Benchmark(Description = "ImageSharp Palette NoDither Png")]
        public void PngCorePaletteNoDither()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            var options = new PngEncoder { Quantizer = new WebSafePaletteQuantizer(new QuantizerOptions { Dither = null }) };
            this.bmpCore.SaveAsPng(memoryStream, options);
        }

        [Benchmark(Description = "ImageSharp Wu Png")]
        public void PngCoreWu()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            var options = new PngEncoder { Quantizer = KnownQuantizers.Wu };
            this.bmpCore.SaveAsPng(memoryStream, options);
        }

        [Benchmark(Description = "ImageSharp Wu NoDither Png")]
        public void PngCoreWuNoDither()
        {
            this.memoryStream.Seek(0, SeekOrigin.Begin);
            var options = new PngEncoder { Quantizer = new WuQuantizer(new QuantizerOptions { Dither = null }), ColorType = PngColorType.Palette };
            this.bmpCore.SaveAsPng(memoryStream, options);
        }
    }
}
