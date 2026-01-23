using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using MicroBenchmarks;

namespace SveBenchmarks
{
    [BenchmarkCategory(Categories.Runtime)]
    [OperatingSystemsArchitectureFilter(allowed: true, System.Runtime.InteropServices.Architecture.Arm64)]
    [Config(typeof(Config))]
    public class ScatterStore
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddFilter(new SimpleFilter(_ => Sve.IsSupported));
            }
        }

        [Params(15, 127, 527, 10015)]
        public int Size;

        private uint[] _objects;
        private uint[] _indices;

        [GlobalSetup]
        public virtual void Setup()
        {
            _objects = new uint[Size];
            _indices = ValuesGenerator.Array<uint>(Size);
            for (int i = 0; i < Size; i++)
            {
                _indices[i] %= (uint)Size;
            }
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            uint[] current = (uint[])_objects.Clone();
            Setup();
            Scalar();
            uint[] scalar = (uint[])_objects.Clone();
            // Check that the result is the same as the scalar result.
            for (int i = 0; i < current.Length; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_019.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (uint* objects = _objects, indices = _indices)
            {
                for (int i = 0; i < Size; i++)
                {
                    objects[indices[i]] = 1;
                }
            }
        }

        [Benchmark]
        public unsafe void SveScatterStore()
        {
            fixed (uint* objects = _objects, indices = _indices)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                Vector<uint> ones = Vector<uint>.One;
                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();
                Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(0, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<uint> idxVec = Sve.LoadVector(pLoop, indices + i);

                    // Set the indices within objects with ones.
                    Sve.Scatter(pLoop, objects, idxVec, ones);

                    i += cntw;
                    pLoop = Sve.CreateWhileLessThanMask32Bit(i, Size);
                }
            }
        }

    }
}
