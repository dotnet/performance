// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Based on the Raytracer example from
// Samples for Parallel Programming with the .NET Framework
// https://code.msdn.microsoft.com/windowsdesktop/Samples-for-Parallel-b4b76364

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Benchmarks.SIMD.RayTracer;

namespace SIMD
{
public class RayTracerBench
{
    private const int Width = 250;
    private const int Height = 250;
    private const int Iterations = 7;

    public RayTracerBench()
    {
        _width = Width;
        _height = Height;
        _parallel = false;
        _showThreads = false;
        _freeBuffers = new ObjectPool<int[]>(() => new int[_width * _height]);
    }

    private bool _parallel;
    private bool _showThreads;
    private int _width, _height;
    private int _degreeOfParallelism = Environment.ProcessorCount;
    private int _frames;
    private CancellationTokenSource _cancellation;
    private ObjectPool<int[]> _freeBuffers;

    private void RenderBench()
    {
        _cancellation = new CancellationTokenSource();
        RenderLoop(Iterations);
    }

    private void RenderLoop(int iterations)
    {
        // Create a ray tracer, and create a reference to "sphere2" that we are going to bounce
        var rayTracer = new RayTracer(_width, _height);
        var scene = rayTracer.DefaultScene;
        var sphere2 = (Sphere)scene.Things[0]; // The first item is assumed to be our sphere
        var baseY = sphere2.Radius;
        sphere2.Center.Y = sphere2.Radius;

        // Timing determines how fast the ball bounces as well as diagnostics frames/second info
        var renderingTime = new Stopwatch();
        var totalTime = Stopwatch.StartNew();

        // Keep rendering until the iteration count is hit
        for (_frames = 0; _frames < iterations; _frames++)
        {
            // Or the rendering task has been canceled
            if (_cancellation.IsCancellationRequested)
            {
                break;
            }

            // Get the next buffer
            var rgb = _freeBuffers.GetObject();

            // Determine the new position of the sphere based on the current time elapsed
            double dy2 = 0.8 * Math.Abs(Math.Sin(totalTime.ElapsedMilliseconds * Math.PI / 3000));
            sphere2.Center.Y = (float)(baseY + dy2);

            // Render the scene
            renderingTime.Reset();
            renderingTime.Start();
            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _degreeOfParallelism,
                CancellationToken = _cancellation.Token
            };
            if (!_parallel) rayTracer.RenderSequential(scene, rgb);
            else if (_showThreads) rayTracer.RenderParallelShowingThreads(scene, rgb, options);
            else rayTracer.RenderParallel(scene, rgb, options);
            renderingTime.Stop();

            _freeBuffers.PutObject(rgb);
        }
    }

    [Benchmark(Description = nameof(RayTracerBench))]
    public void Bench() => RenderBench();
}
}
