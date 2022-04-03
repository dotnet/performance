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
            // in the vectorized pathes.

            if (typeof(T) == typeof(byte))
            {
                // Vectorization is done on 2 * Vector<byte>.Count => 64 byte elements
                yield return 63;    // one less the vectorization threshould
                yield return 64;    // exactly two vectorized operations
                yield return 65;    // one element more than standard vectorized loop
                yield return 95;    // one element less than another iteration of the standard vectorized loop
                yield return 100;
            }
            else if (typeof(T) == typeof(char))
            {
                // Vectorization is done on 2 * Vector<char>.Count => 32 char elements
                // Values analogous to the byte values above
                yield return 31;
                yield return 32;
                yield return 33;
                yield return 47;
                yield return 100;
            }
            else
            {
                throw new NotSupportedException();
            }
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
