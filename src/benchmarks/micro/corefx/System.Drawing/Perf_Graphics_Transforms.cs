// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Drawing.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Graphics_Transforms
    {
        private Bitmap _image;
        private Graphics _graphics;
        private Point[] _points;

        [GlobalSetup]
        public void Setup()
        {
            _points = new []
            {
                new Point(10, 10), new Point(20, 1), new Point(35, 5), new Point(50, 10),
                new Point(60, 15), new Point(65, 25), new Point(50, 30)
            };

            _image = new Bitmap(100, 100);
            _graphics = Graphics.FromImage(_image);
        }

        [Benchmark]
        [AllowedOperatingSystems("Graphics.TransformPoints is not implemented in libgdiplus yet. See dotnet/corefx 20884", OS.Windows)]
        public void TransformPoints()
        {
            _graphics.TransformPoints(CoordinateSpace.World, CoordinateSpace.Page, _points);
            _graphics.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, _points);
            _graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Device, _points);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _graphics.Dispose();
            _image.Dispose();
        }
    }
}
