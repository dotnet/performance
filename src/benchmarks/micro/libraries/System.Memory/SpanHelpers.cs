// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(char))]
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Span)]
    [ShortRunJob]
    public unsafe class SpanHelpers<T>
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        private T* _searchSpace;
        private T _value;

        [ParamsSource(nameof(LengthValues))]
        public int Length { get; set; }

        public static IEnumerable<object> LengthValues()
        {
            // The values for the length take into account the different cut-offs
            // in the vectorized paths.
            int V = Vector<T>.Count;

            yield return 2 * V - 1;    // one less than the vectorization threshold
            yield return 2 * V;        // exactly two vectorized operations
            yield return 2 * V + 1;    // one element more than standard vectorized loop
            yield return 3 * V - 1;    // one element less than another iteration of the standard vectorized loop
            yield return 100;
        }

        [GlobalSetup]
        public void Setup()
        {
            _searchSpace = (T*)NativeMemory.AlignedAlloc((uint)Length, 32);
            Debug.Assert((nint)_searchSpace % Vector<T>.Count == 0);
            Unsafe.InitBlock(_searchSpace, 0x00, (uint)Length);

            _value = ValuesGenerator.GetNonDefaultValue<T>();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (_searchSpace != null)
            {
                NativeMemory.AlignedFree(_searchSpace);
                _searchSpace = null;
            }
        }

        [Benchmark]
        public bool Contains()
        {
            return new System.Span<T>(_searchSpace, Length).Contains(_value);
        }
    }
}
