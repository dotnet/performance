using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Tests
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(char))]
    [BenchmarkCategory(Categories.Runtime)]
    public class Perf_GC<T>
    {
        [Benchmark]
        [Arguments(1000)]
        [Arguments(10_000)]
[MemoryRandomization]
        public T[] NewOperator_Array(int length) => new T[length];

#if NET5_0_OR_GREATER // API introduced in .NET 5
        public static IEnumerable<object[]> GetArguments()
        {
            foreach (int length in new [] { 1000, 10_000 }) // both test cases excercise different code paths
                foreach (bool pinned in new[] { true, false })
                    yield return new object[] { length, pinned };
        }

        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public T[] AllocateArray(int length, bool pinned) => GC.AllocateArray<T>(length, pinned);

        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public T[] AllocateUninitializedArray(int length, bool pinned) => GC.AllocateUninitializedArray<T>(length, pinned);
#endif
    }
}