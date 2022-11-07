// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberHidesStaticFromOuterClass
namespace SixLabors.ImageSharp.Tests
{
    /// <summary>
    /// Class that contains all the relative test image paths in the TestImages/Formats directory.
    /// Use with <see cref="WithFileAttribute"/>, <see cref="WithFileCollectionAttribute"/>.
    /// </summary>
    public static class TestImages
    {
        public static class Png
        {
            public const string Splash = "Png/splash.png";
            public const string Rgb48Bpp = "Png/rgb-48bpp.png";

            // Filtered test images from http://www.schaik.com/pngsuite/pngsuite_fil_png.html
            public const string Filter0 = "Png/filter0.png";
            public const string Filter1 = "Png/filter1.png";
            public const string Filter2 = "Png/filter2.png";
            public const string Filter3 = "Png/filter3.png";
            public const string Filter4 = "Png/filter4.png";
        }

        public static class Jpeg
        {
            public static class Baseline
            {
                public const string Lake = "Jpg/baseline/Lake.jpg";
                public const string Jpeg420Exif = "Jpg/baseline/jpeg420exif.jpg";
                public const string HistogramEqImage = "Jpg/baseline/640px-Unequalized_Hawkes_Bay_NZ.jpg";
                public const string Winter444_Interleaved = "Jpg/baseline/winter444_interleaved.jpg";
                public const string Hiyamugi = "Jpg/baseline/Hiyamugi.jpg";
                public const string Jpeg400 = "Jpg/baseline/jpeg400jfif.jpg";
                public const string Snake = "Jpg/baseline/Snake.jpg";
            }

            public static class Issues
            {
                public const string BadRstProgressive518 = "Jpg/issues/Issue518-Bad-RST-Progressive.jpg";
            }

            public static class Progressive
            {
                public const string Winter420_NonInterleaved = "Jpg/progressive/winter420_noninterleaved.jpg";
            }

            public static class BenchmarkSuite
            {
                //    public const string Jpeg400_SmallMonochrome = Baseline.Jpeg400;
                public const string Jpeg420Exif_MidSizeYCbCr = Baseline.Jpeg420Exif;
                public const string Lake_Small444YCbCr = Baseline.Lake;

                //    // A few large images from the "issues" set are actually very useful for benchmarking:
                public const string BadRstProgressive518_Large444YCbCr = Issues.BadRstProgressive518;
            }
        }

        public static class Bmp
        {
            public const string Car = "Bmp/Car.bmp";
        }

        public static class Gif
        {
            public const string Rings = "Gif/rings.gif";
        }

        public static class Tga
        {
            public const string Bit24BottomLeft = "Tga/targa_24bit.tga";
        }
    }
}
