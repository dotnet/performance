// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Linq;

namespace System.Numerics.Tensors.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    [GenericTypeArguments(typeof(float))]
    [GenericTypeArguments(typeof(double))]
    public class Perf_FloatingPointTensorPrimitives<T>
        where T : unmanaged, IFloatingPointIeee754<T>
    {
        [Params(128, 6 * 512 + 7)]
        public int BufferLength;

        private T[] _source1;
        private T[] _source2;
        private T[] _source3;
        private T _scalar1;
        private T[] _ones;
        private T[] _destination;

        [GlobalSetup]
        public void Init()
        {
            _source1 = ValuesGenerator.Array<T>(BufferLength, seed: 42);
            _source2 = ValuesGenerator.Array<T>(BufferLength, seed: 43);
            _source3 = ValuesGenerator.Array<T>(BufferLength, seed: 44);
            _scalar1 = ValuesGenerator.Value<T>(seed: 45);
            _ones = Enumerable.Repeat(T.One, BufferLength).ToArray();
            _destination = new T[BufferLength];
        }

        #region Unary Operations
        [Benchmark]
        public void AtanPi() => TensorPrimitives.AtanPi<T>(_source1, _destination);

        [Benchmark]
        public void Exp() => TensorPrimitives.Exp<T>(_source1, _destination);

        [Benchmark]
        public void Log() => TensorPrimitives.Log<T>(_source1, _destination);

        [Benchmark]
        public void Round() => TensorPrimitives.Round<T>(_source1, _destination);

        [Benchmark]
        public void Sin() => TensorPrimitives.Sin<T>(_source1, _destination);

        [Benchmark]
        public void Sinh() => TensorPrimitives.Sinh<T>(_source1, _destination);

        [Benchmark]
        public void Sigmoid() => TensorPrimitives.Sigmoid<T>(_source1, _destination);

        [Benchmark]
        public void Sqrt() => TensorPrimitives.Sqrt<T>(_source1, _destination);

        [Benchmark]
        public void Truncate() => TensorPrimitives.Truncate<T>(_source1, _destination);
        #endregion

        #region Binary/Ternary Operations
        [Benchmark]
        public void FusedMultiplyAdd_Vectors() => TensorPrimitives.FusedMultiplyAdd<T>(_source1, _source2, _source3, _destination);

        [Benchmark]
        public void FusedMultiplyAdd_ScalarAddend() => TensorPrimitives.FusedMultiplyAdd(_source1, _scalar1, _source2, _destination);

        [Benchmark]
        public void FusedMultiplyAdd_ScalarMultiplier() => TensorPrimitives.FusedMultiplyAdd(_source1, _source2, _scalar1, _destination);

        [Benchmark]
        public void Ieee754Remainder_Vector() => TensorPrimitives.Ieee754Remainder<T>(_source1, _source2, _destination);

        [Benchmark]
        public void Ieee754Remainder_ScalarDividend() => TensorPrimitives.Ieee754Remainder(_scalar1, _source1, _destination);

        [Benchmark]
        public void Ieee754Remainder_ScalarDivisor() => TensorPrimitives.Ieee754Remainder(_source1, _scalar1, _destination);

        [Benchmark]
        public void Pow_Vectors() => TensorPrimitives.Pow<T>(_source1, _ones, _destination);

        [Benchmark]
        public void Pow_ScalarBase() => TensorPrimitives.Pow(_scalar1, _ones, _destination);

        [Benchmark]
        public void Pow_ScalarExponent() => TensorPrimitives.Pow(_source1, T.One, _destination);
        #endregion

        #region Reducers
        [Benchmark]
        public T CosineSimilarity() => TensorPrimitives.CosineSimilarity<T>(_source1, _source2);

        [Benchmark]
        public T Distance() => TensorPrimitives.Distance<T>(_source1, _source2);
        #endregion
    }
}
