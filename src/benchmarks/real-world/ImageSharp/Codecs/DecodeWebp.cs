// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using BenchmarkDotNet.Attributes;

using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    [MarkdownExporter]
    [HtmlExporter]
    [Config(typeof(Config.ShortMultiFramework))]
    public class DecodeWebp
    {
        private Configuration configuration;

        private byte[] webpLossyBytes;

        private byte[] webpLosslessBytes;

        private string TestImageLossyFullPath => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImageLossy);

        private string TestImageLosslessFullPath => Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, this.TestImageLossless);

        [Params(TestImages.Webp.Lossy.Earth)]
        public string TestImageLossy { get; set; }

        [Params(TestImages.Webp.Lossless.Earth)]
        public string TestImageLossless { get; set; }

        [GlobalSetup]
        public void ReadImages()
        {
            this.webpLossyBytes ??= File.ReadAllBytes(this.TestImageLossyFullPath);
            this.webpLosslessBytes ??= File.ReadAllBytes(this.TestImageLosslessFullPath);
        }

        [Benchmark(Description = "ImageSharp Lossy Webp")]
        public int WebpLossy()
        {
            using var memoryStream = new MemoryStream(this.webpLossyBytes);
            using var image = Image.Load<Rgba32>(this.configuration, memoryStream);
            return image.Height;
        }

        [Benchmark(Description = "ImageSharp Lossless Webp")]
        public int WebpLossless()
        {
            using var memoryStream = new MemoryStream(this.webpLosslessBytes);
            using var image = Image.Load<Rgba32>(this.configuration, memoryStream);
            return image.Height;
        }
    }
}
