// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp.Formats.Bmp;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    [Config(typeof(Config.ShortMultiFramework))]
    public class EncodeBmpMultiple : MultiImageBenchmarkBase.WithImagesPreloaded
    {
        protected override IEnumerable<string> InputImageSubfoldersOrFiles => new[] { "Bmp/", "Jpg/baseline" };

        [Benchmark(Description = "EncodeBmpMultiple - ImageSharp")]
        public void EncodeBmpImageSharp()
            => this.ForEachImageSharpImage((img, ms) =>
            {
                img.Save(ms, new BmpEncoder());
                return null;
            });
    }
}
