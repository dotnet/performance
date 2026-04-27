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
    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(float))]
    [GenericTypeArguments(typeof(double))]
    public class Perf_NumberTensorPrimitives<T>
        where T : INumber<T>, IBitwiseOperators<T, T, T>
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
        public void Abs() => TensorPrimitives.Abs<T>(_source1, _destination);

        [Benchmark]
        public void Negate() => TensorPrimitives.Negate<T>(_source1, _destination);
        #endregion

        #region Binary/Ternary Operations
        [Benchmark]
        public void Add_Vector() => TensorPrimitives.Add<T>(_source1, _source2, _destination);

        [Benchmark]
        public void Add_Scalar() => TensorPrimitives.Add(_source1, _scalar1, _destination);

        [Benchmark]
        public void AddMultiply_Vectors() => TensorPrimitives.AddMultiply<T>(_source1, _source2, _source3, _destination);

        [Benchmark]
        public void AddMultiply_ScalarAddend() => TensorPrimitives.AddMultiply(_source1, _scalar1, _source2, _destination);

        [Benchmark]
        public void AddMultiply_ScalarMultiplier() => TensorPrimitives.AddMultiply(_source1, _source2, _scalar1, _destination);

        [Benchmark]
        public void BitwiseAnd_Vector() => TensorPrimitives.BitwiseAnd<T>(_source1, _source2, _destination);

        [Benchmark]
        public void BitwiseAnd_Scalar() => TensorPrimitives.BitwiseAnd(_source1, _scalar1, _destination);

        [Benchmark]
        public void Divide_Vector() => TensorPrimitives.Divide<T>(_source1, _ones, _destination);

        [Benchmark]
        public void Divide_Scalar() => TensorPrimitives.Divide(_source1, T.One, _destination);

        [Benchmark]
        public void Max_Vector() => TensorPrimitives.Max<T>(_source1, _source2, _destination);

        [Benchmark]
        public void Max_Scalar() => TensorPrimitives.Max(_source1, _scalar1, _destination);

        [Benchmark]
        public void MaxMagnitude_Vector() => TensorPrimitives.MaxMagnitude<T>(_source1, _source2, _destination);

        [Benchmark]
        public void MaxMagnitude_Scalar() => TensorPrimitives.MaxMagnitude(_source1, _scalar1, _destination);
        #endregion

        #region Reducers
        [Benchmark]
        public T Max() => TensorPrimitives.Max<T>(_source1);

        [Benchmark]
        public T SumOfMagnitudes() => TensorPrimitives.SumOfMagnitudes<T>(_ones);

        [Benchmark]
        public T SumOfSquares() => TensorPrimitives.SumOfSquares<T>(_ones);

        [Benchmark]
        public int IndexOfMax() => TensorPrimitives.IndexOfMax<T>(_source1);
        #endregion
    }
}
