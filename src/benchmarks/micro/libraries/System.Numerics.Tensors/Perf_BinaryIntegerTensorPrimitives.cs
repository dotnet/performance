// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Numerics.Tensors.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    [GenericTypeArguments(typeof(int))]
    public class Perf_BinaryIntegerTensorPrimitives<T>
        where T : unmanaged, IBinaryInteger<T>
    {
        [Params(128, 6 * 512 + 7)]
        public int BufferLength;

        private T[] _source1;
        private T[] _destination;

        [GlobalSetup]
        public void Init()
        {
            _source1 = ValuesGenerator.Array<T>(BufferLength, seed: 42);
            _destination = new T[BufferLength];
        }

        #region Unary Operations
        [Benchmark]
        public void LeadingZeroCount() => TensorPrimitives.LeadingZeroCount<T>(_source1, _destination);

        [Benchmark]
        public void OnesComplement() => TensorPrimitives.OnesComplement<T>(_source1, _destination);

        [Benchmark]
        public void PopCount() => TensorPrimitives.PopCount<T>(_source1, _destination);

        [Benchmark]
        public void ShiftLeft() => TensorPrimitives.ShiftLeft<T>(_source1, shiftAmount: 3, _destination);

        [Benchmark]
        public void TrailingZeroCount() => TensorPrimitives.TrailingZeroCount<T>(_source1, _destination);
        #endregion
    }
}