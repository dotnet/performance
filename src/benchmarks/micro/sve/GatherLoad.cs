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
    public class GatherLoad
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
        private ulong _result;

        [GlobalSetup]
        public virtual void Setup()
        {
            _objects = ValuesGenerator.Array<uint>(Size);
            _indices = ValuesGenerator.Array<uint>(Size);
            for (int i = 0; i < Size; i++)
            {
                _indices[i] %= (uint)Size;
            }
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            ulong current = _result;
            Setup();
            Scalar();
            ulong scalar = _result;
            // Check that the result is the same as the scalar result.
            Debug.Assert(current == scalar);
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_023.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (uint* objects = _objects, indices = _indices)
            {
                ulong res = 0;
                for (int i = 0; i < Size; i++)
                {
                    res += objects[indices[i]];
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void SveGatherLoad()
        {
            fixed (uint* objects = _objects, indices = _indices)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                Vector<uint> ones = Vector<uint>.One;
                Vector<uint> resVec = Vector<uint>.Zero;
                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();
                Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(0, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Load indices
                    Vector<uint> idxVec = Sve.LoadVector(pLoop, indices + i);
                    // Gather elements from objects using indices.
                    Vector<uint> objVec = Sve.GatherVector(pLoop, objects, idxVec);
                    // Add results to resVec.
                    resVec = Sve.Add(resVec, objVec);

                    i += cntw;
                    pLoop = Sve.CreateWhileLessThanMask32Bit(i, Size);
                }
                // Add up all elements in resVec.
                ulong res = (ulong)Sve.AddAcross(resVec).ToScalar();
                _result = res;
            }
        }

    }
}
