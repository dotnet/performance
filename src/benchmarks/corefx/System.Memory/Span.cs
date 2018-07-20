using System.Collections;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(int))]
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class Span<T> 
        where T : struct 
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _array;

        [GlobalSetup]
        public void Setup() => _array = ValuesGenerator.Array<T>(Size);
        
        [Benchmark]
        public System.Span<T> Slice() => new System.Span<T>(_array).Slice(Size / 2);

        [Benchmark]
        public void Clear() => new System.Span<T>(_array).Clear();
        
        [Benchmark]
        public void Fill() => new System.Span<T>(_array).Fill(default);

        [Benchmark]
        public T[] ToArray() => new System.Span<T>(_array).ToArray();
    }
}