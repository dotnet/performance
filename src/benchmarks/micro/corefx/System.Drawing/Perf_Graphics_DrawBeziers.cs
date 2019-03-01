// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Drawing.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Graphics_DrawBeziers
    {
        private Bitmap _image;
        private Pen _pen;
        private Graphics _graphics;
        private Point _point1, _point2, _point3, _point4;
        private Point[] _points;

        [GlobalSetup]
        public void Setup()
        {
            _image = new Bitmap(100, 100);
            _pen = new Pen(Color.White);
            _graphics = Graphics.FromImage(_image);

            _point1 = new Point(10, 10);
            _point2 = new Point(20, 1);
            _point3 = new Point(35, 5);
            _point4 = new Point(50, 10);

            _points = new[] {_point1, _point2, _point3, _point4};
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _graphics.Dispose();
            _pen.Dispose();
            _image.Dispose();
        }

        [Benchmark]
        public void DrawBezier_Point() => _graphics.DrawBezier(_pen, _point1, _point2, _point3, _point4);

        [Benchmark]
        public void DrawBezier_Points() => _graphics.DrawBeziers(_pen, _points);
    }
}
