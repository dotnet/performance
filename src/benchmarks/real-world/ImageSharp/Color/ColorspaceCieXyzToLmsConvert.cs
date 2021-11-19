// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;

using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;

namespace SixLabors.ImageSharp.Benchmarks.ColorSpaces
{
    public class ColorspaceCieXyzToLmsConvert
    {
        private static readonly CieXyz CieXyz = new CieXyz(0.95047F, 1, 1.08883F);

        private static readonly ColorSpaceConverter ColorSpaceConverter = new ColorSpaceConverter();

        [Benchmark(Description = "ImageSharp Convert")]
        public float ColorSpaceConvert()
        {
            return ColorSpaceConverter.ToLms(CieXyz).L;
        }
    }
}
