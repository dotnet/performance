// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Drawing.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Image_Load
    {
        // this field is lazy to avoid the exception during static ctor initialization of this type (harder to catch and handle properly)
        private static readonly Lazy<ImageTestData[]> LazyTestCases = new Lazy<ImageTestData[]>(CreateTestCases);

        public IEnumerable<object> ImageFormats() => LazyTestCases.Value;

        [Benchmark]
        [ArgumentsSource(nameof(ImageFormats))]
        public void Bitmap_FromStream(ImageTestData format)
        {
            using (new Bitmap(format.Stream))
            {
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ImageFormats))]
        public void Image_FromStream(ImageTestData format)
        {
            using (Image.FromStream(format.Stream))
            {
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ImageFormats))]
        public void Image_FromStream_NoValidation(ImageTestData format)
        {
            using (Image.FromStream(format.Stream, false, false))
            {
            }
        }

        private static ImageTestData[] CreateTestCases()
        {
            try
            {
                return new [] 
                {
                    new ImageTestData(ImageFormat.Bmp),
                    new ImageTestData(ImageFormat.Jpeg),
                    new ImageTestData(ImageFormat.Png),
                    new ImageTestData(ImageFormat.Gif)
                };
            }
            catch (Exception) when (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("libgdiplus is missing, you can install it by running 'apt-get install libgdiplus'");
                Console.ResetColor();

                return Array.Empty<ImageTestData>();
            }
        }

        public class ImageTestData
        {
            public Stream Stream { get; }
            private string FormatName { get; }

            public ImageTestData(ImageFormat format)
            {
                Stream = CreateTestImage(format);
                FormatName = format.ToString();
            }

            // the value returned by ToString is used in the text representation of Benchmark ID in our reporting system
            public override string ToString() => FormatName;

            private static Stream CreateTestImage(ImageFormat format)
            {
                Random r = new Random(1066); // the seed must not be changed

                const int Size = 1000;
                Point RandomPoint() => new Point(r.Next(Size), r.Next(Size));

                var result = new MemoryStream();

                using (Bitmap bitmap = new Bitmap(Size, Size))
                using (Pen pen = new Pen(Color.Blue))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        graphics.DrawBezier(pen, RandomPoint(), RandomPoint(), RandomPoint(), RandomPoint());
                    }

                    bitmap.Save(result, format);
                }

                return result;
            }
        }
    }
}
