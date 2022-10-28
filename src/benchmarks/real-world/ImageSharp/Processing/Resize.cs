// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Tests;

namespace SixLabors.ImageSharp.Benchmarks
{
    public abstract class Resize<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        private byte[] bytes = null;

        private Image<TPixel> sourceImage;


        protected Configuration Configuration { get; } = new Configuration(new JpegConfigurationModule());

        protected int DestSize { get; private set; }

        [GlobalSetup]
        public virtual void Setup()
        {
            if (this.bytes is null)
            {
                this.bytes = File.ReadAllBytes(Path.Combine(TestEnvironment.InputImagesDirectoryFullPath, TestImages.Jpeg.Baseline.Snake));

                this.sourceImage = Image.Load<TPixel>(this.bytes);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            this.bytes = null;
            this.sourceImage.Dispose();
        }

        [Benchmark(Description = "ImageSharp")]
        public int ImageSharp_P1() => this.RunImageSharpResize();

        protected int RunImageSharpResize()
        {
            this.Configuration.MaxDegreeOfParallelism = 1;

            using (Image<TPixel> clone = this.sourceImage.Clone(this.ExecuteResizeOperation))
            {
                return clone.Width;
            }
        }

        protected abstract void ExecuteResizeOperation(IImageProcessingContext ctx);
    }

    public class Resize_Bicubic_Rgba32 : Resize<Rgba32>
    {
        protected override void ExecuteResizeOperation(IImageProcessingContext ctx)
            => ctx.Resize(this.DestSize, this.DestSize, KnownResamplers.Bicubic);
    }

    public class Resize_Bicubic_Rgb24 : Resize<Rgb24>
    {
        protected override void ExecuteResizeOperation(IImageProcessingContext ctx)
            => ctx.Resize(this.DestSize, this.DestSize, KnownResamplers.Bicubic);
    }

    public class Resize_Bicubic_Compare_Rgba32_Rgb24
    {
        private Resize_Bicubic_Rgb24 rgb24;
        private Resize_Bicubic_Rgba32 rgba32;

        [GlobalSetup]
        public void Setup()
        {
            this.rgb24 = new Resize_Bicubic_Rgb24();
            this.rgb24.Setup();
            this.rgba32 = new Resize_Bicubic_Rgba32();
            this.rgba32.Setup();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            this.rgb24.Cleanup();
            this.rgba32.Cleanup();
        }

        [Benchmark(Baseline = true)]
        public void Rgba32() => this.rgba32.ImageSharp_P1();

        [Benchmark]
        public void Rgb24() => this.rgb24.ImageSharp_P1();
    }
}