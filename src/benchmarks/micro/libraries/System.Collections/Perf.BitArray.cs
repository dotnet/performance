// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections)]
    public class Perf_BitArray
    {
        private const int DefaultShiftCount = 17;
        private const bool BooleanValue = true;
        // 4 - Small size to test non-vectorised paths
        // DefaultCollectionSize - Big enough size to go through the vectorised paths
        [Params(4, Utils.DefaultCollectionSize)]
        public int Size { get; set; }

        private BitArray _original;
        private BitArray _original2;
        private byte[] _bytes;
        private bool[] _bools;
        private int[] _ints;

        [Benchmark]
        public BitArray BitArrayLengthCtor() => new BitArray(Size);

        [Benchmark]
        public BitArray BitArrayLengthValueCtor() => new BitArray(Size, BooleanValue);

        [GlobalSetup(Target = nameof(BitArrayBitArrayCtor))]
        public void Setup_BitArrayBitArrayCtor() => _original = new BitArray(Size, BooleanValue);

        [Benchmark]
        [MemoryRandomization]
        public BitArray BitArrayBitArrayCtor() => new BitArray(_original);

        [GlobalSetup(Target = nameof(BitArrayBoolArrayCtor))]
        public void Setup_BitArrayBoolArrayCtor() => _bools = ValuesGenerator.Array<bool>(Size);

        [Benchmark]
        public BitArray BitArrayBoolArrayCtor() => new BitArray(_bools);

        [GlobalSetup(Targets = new [] { nameof(BitArrayByteArrayCtor), nameof(BitArraySetLengthGrow), nameof(BitArraySetLengthShrink) })]
        public void Setup_BitArrayByteArrayCtor() => _bytes = ValuesGenerator.Array<byte>(Size);

        [Benchmark]
        public BitArray BitArrayByteArrayCtor() => new BitArray(_bytes);

        [GlobalSetup(Target = nameof(BitArrayIntArrayCtor))]
        public void Setup_BitArrayIntArrayCtor() => _ints = ValuesGenerator.Array<int>(Size);

        [Benchmark]
        [MemoryRandomization]
        public BitArray BitArrayIntArrayCtor() => new BitArray(_ints);

        [GlobalSetup(Targets = new [] { nameof(BitArraySetAll), nameof(BitArrayNot), nameof(BitArrayGet) })]
        public void Setup_BitArraySetAll() => _original = new BitArray(ValuesGenerator.Array<byte>(Size));

        [Benchmark]
        [MemoryRandomization]
        public void BitArraySetAll() => _original.SetAll(BooleanValue);

        [Benchmark]
        public BitArray BitArrayNot() => _original.Not();

        [Benchmark]
        public bool BitArrayGet()
        {
            bool local = false;

            BitArray original = _original;
            for (int j = 0; j < original.Length; j++)
                local ^= original.Get(j);

            return local;
        }

#if !NETFRAMEWORK // API added in .NET Core 2.0
        [GlobalSetup(Targets = new [] { nameof(BitArrayRightShift), nameof(BitArrayLeftShift) })]
        public void Setup_BitArrayShift() => _original = new BitArray(ValuesGenerator.Array<byte>(Size));

        [Benchmark]
        public void BitArrayRightShift() => _original.RightShift(DefaultShiftCount);

        [Benchmark]
        public void BitArrayLeftShift() => _original.LeftShift(DefaultShiftCount);
#endif

        [GlobalSetup(Targets = new [] { nameof(BitArrayAnd), nameof(BitArrayOr), nameof(BitArrayXor) })]
        public void Setup_BitArrayAnd()
        {
            _original = new BitArray(ValuesGenerator.Array<byte>(Size));
            _original2 = new BitArray(ValuesGenerator.Array<byte>(Size));
        }

        [Benchmark]
        public BitArray BitArrayAnd() => _original.And(_original2);

        [Benchmark]
        public BitArray BitArrayOr() => _original.Or(_original2);

        [Benchmark]
        public BitArray BitArrayXor() => _original.Xor(_original2);

        [GlobalSetup(Target = nameof(BitArraySet))]
        public void Setup_BitArraySet() => _original = new BitArray(ValuesGenerator.Array<bool>(Size));

        [Benchmark]
        public void BitArraySet()
        {
            BitArray original = _original;

            for (int j = 0; j < original.Length; j++)
                original.Set(j, BooleanValue);
        }

        [Benchmark]
        [MemoryRandomization]
        public BitArray BitArraySetLengthGrow()
        {
            var original = new BitArray(_bytes);
            original.Length = original.Length * 2;
            return original;
        }

        [Benchmark]
        public BitArray BitArraySetLengthShrink()
        {
            var original = new BitArray(_bytes);
            original.Length = original.Length / 2;
            return original;
        }

        [GlobalSetup(Target = nameof(BitArrayCopyToIntArray))]
        public void Setup_BitArrayCopyToIntArray()
        {
            _bytes = ValuesGenerator.Array<byte>(Size);
            _original = new BitArray(_bytes);
            _ints = new int[Size / 4];
        }

        [Benchmark]
        [MemoryRandomization]
        public void BitArrayCopyToIntArray() => _original.CopyTo(_ints, 0);

        [GlobalSetup(Target = nameof(BitArrayCopyToByteArray))]
        public void Setup_BitArrayCopyToByteArray()
        {
            _bytes = ValuesGenerator.Array<byte>(Size);
            _original = new BitArray(_bytes);
        }

        [Benchmark]
        public void BitArrayCopyToByteArray() => _original.CopyTo(_bytes, 0);

        [GlobalSetup(Target = nameof(BitArrayCopyToBoolArray))]
        public void Setup_BitArrayCopyToBoolArray()
        {
            _bytes = ValuesGenerator.Array<byte>(Size);
            _original = new BitArray(_bytes);
            _bools = new bool[Size * 32];
        }

        [Benchmark]
        public void BitArrayCopyToBoolArray() => _original.CopyTo(_bools, 0);
    }
}
