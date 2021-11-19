// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;

using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;

namespace SixLabors.ImageSharp.Benchmarks.ColorSpaces
{
    public class RgbWorkingSpaceAdapt
    {
        private static readonly Rgb Rgb = new Rgb(0.206162F, 0.260277F, 0.746717F, RgbWorkingSpaces.WideGamutRgb);

        private static readonly ColorSpaceConverter ColorSpaceConverter = new ColorSpaceConverter(new ColorSpaceConverterOptions { TargetRgbWorkingSpace = RgbWorkingSpaces.SRgb });

        [Benchmark(Description = "ImageSharp Adapt")]
        public Rgb ColorSpaceConvert()
        {
            return ColorSpaceConverter.Adapt(Rgb);
        }
    }
}
