// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Tests;

// ReSharper disable InconsistentNaming
namespace SixLabors.ImageSharp.Benchmarks.Codecs.Jpeg
{
    /// <summary>
    /// Image-specific Jpeg benchmarks
    /// </summary>
    
    public class DecodeJpeg_ImageSpecific
    {
        private byte[] jpegBytes;

        private string TestImageFullPath => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImage);

#pragma warning disable SA1115
        [Params(
            TestImages.Jpeg.BenchmarkSuite.Lake_Small444YCbCr,
            TestImages.Jpeg.BenchmarkSuite.BadRstProgressive518_Large444YCbCr,

            // The scaled result for the large image "ExifGetString750Transform_Huge420YCbCr"
            // is almost the same as the result for Jpeg420Exif,
            // which proves that the execution time for the most common YCbCr 420 path scales linearly.
            // TestImages.Jpeg.BenchmarkSuite.ExifGetString750Transform_Huge420YCbCr,
            TestImages.Jpeg.BenchmarkSuite.Jpeg420Exif_MidSizeYCbCr)]

        public string TestImage { get; set; }

        [GlobalSetup]
        public void ReadImages()
        {
            if (this.jpegBytes == null)
            {
                this.jpegBytes = File.ReadAllBytes(this.TestImageFullPath);
            }
        }

        [Benchmark]
        public Size ImageSharp()
        {
            using (var memoryStream = new MemoryStream(this.jpegBytes))
            {
                using (var image = Image.Load(memoryStream, new JpegDecoder { IgnoreMetadata = true }))
                {
                    return new Size(image.Width, image.Height);
                }
            }
        }
    }
}
