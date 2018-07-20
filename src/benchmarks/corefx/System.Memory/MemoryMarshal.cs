using System.Collections;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(int))]
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class MemoryMarshal<T>
        where T : struct
    {
        private T[] _array = UniqueValuesGenerator.GenerateArray<T>(Utils.DefaultCollectionSize);
        
        [Benchmark]
        public ref T MemoryMarshalGetReference() => ref MemoryMarshal.GetReference(new System.Span<T>(_array));
        
        [Benchmark]
        public System.Span<byte> AsBytes() => MemoryMarshal.AsBytes(new System.Span<T>(_array));

        [Benchmark]
        public System.Span<byte> CastToByte() => MemoryMarshal.Cast<T, byte>(new System.Span<T>(_array));
        
        [Benchmark]
        public System.Span<int> CastToInt() => MemoryMarshal.Cast<T, int>(new System.Span<T>(_array));
    }
}