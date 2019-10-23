using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Reflection;

namespace System.Tests
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(char))]
    [BenchmarkCategory(Categories.CoreCLR)]
    public class Perf_GC<T>
    {
        [Benchmark]
        [Arguments(512)]
        [Arguments(512 * 8)]
        public T[] NewOperator_Array(int length) => new T[length];

#if !NETFRAMEWORK && !NETCOREAPP2_1 && !NETCOREAPP2_2// API added in .NET Core 3.0 (internal)
        private readonly Func<int, T[]> _allocateUninitializedArrayDelegate = CreateDelegate(typeof(GC), "AllocateUninitializedArray");

        [Benchmark]
        [Arguments(512)] // less than GC.AllocateUninitializedArray threshold
        [Arguments(512 * 8)] // more than GC.AllocateUninitializedArray threshold
        public T[] AllocateUninitializedArray(int length) => _allocateUninitializedArrayDelegate(length);

        private static Func<int, T[]> CreateDelegate(Type type, string methodName)
        {
            // this method is not a part of .NET Standard so we need to use reflection
            var method = type
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(T));

            return method != null ? (Func<int, T[]>)method.CreateDelegate(typeof(Func<int, T[]>)) : null;
        }
#endif
    }
}
