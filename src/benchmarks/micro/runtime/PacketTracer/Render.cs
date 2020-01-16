// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace HardwareIntrinsics.RayTracer
{
    [BenchmarkCategory(Categories.Runtime)]
    public class SoA
    {
        private const int RunningTime = 1000;
        private const int Width = 248;
        private const int Height = 248;

        private int[] rgbBuffer = new int[Width * 3 * Height]; // Each pixel has 3 fields (RGB)

        [GlobalSetup]
        public unsafe void Setup() => Render(); // run it once during the Setup to avoid https://github.com/dotnet/BenchmarkDotNet/issues/837s

        [Benchmark]
        public unsafe void Render()
        {
            if (!Avx2.IsSupported)
                return;

            // Create a ray tracer, and create a reference to "sphere2" that we are going to bounce
            var packetTracer = new Packet256Tracer(Width, Height);
            var scene = packetTracer.DefaultScene;
            var sphere2 = (SpherePacket256)scene.Things[0]; // The first item is assumed to be our sphere
            var baseY = sphere2.Radiuses;
            sphere2.Centers.Ys = sphere2.Radiuses;

            float dy2 = 0.8f * MathF.Abs(MathF.Sin((float) (1 * Math.PI / 3000)));
            sphere2.Centers.Ys = Avx.Add(baseY, Vector256.Create(dy2));

            fixed (int* ptr = rgbBuffer)
            {
                packetTracer.RenderVectorized(scene, ptr);
            }
        }
    }
}