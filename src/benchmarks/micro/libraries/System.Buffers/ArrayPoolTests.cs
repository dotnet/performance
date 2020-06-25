// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Buffers.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    [GenericTypeArguments(typeof(byte))] // value type
    [GenericTypeArguments(typeof(object))] // reference type
    public class RentReturnArrayPoolTests<T>
    {
        private readonly ArrayPool<T> _createdPool = ArrayPool<T>.Create();
        private readonly T[][] _nestedArrays = new T[NestedDepth][];
        private const int Iterations = 100_000;
        private const int NestedDepth = 8;
        [Params(4096)]
        public int RentalSize;

        [Params(false, true)]
        public bool ManipulateArray { get; set; }

        [Params(false, true)]
        public bool Async { get; set; }

        [Params(false, true)]
        public bool UseSharedPool { get; set; }

        private ArrayPool<T> Pool => UseSharedPool ? ArrayPool<T>.Shared : _createdPool;

        private static void Clear(T[] arr) => arr.AsSpan().Clear();

        private static T IterateAll(T[] arr)
        {
            T ret = default;
            foreach (T item in arr)
            {
                ret = item;
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public async Task SingleSerial()
        {
            ArrayPool<T> pool = Pool;
            for (int i = 0; i < Iterations; i++)
            {
                T[] arr = pool.Rent(RentalSize);
                if (ManipulateArray) Clear(arr);
                if (Async) await Task.Yield();
                if (ManipulateArray) IterateAll(arr);
                pool.Return(arr);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public async Task SingleParallel()
        {
            ArrayPool<T> pool = Pool;
            await Task.WhenAll(
                Enumerable
                .Range(0, Environment.ProcessorCount)
                .Select(_ =>
                Task.Run(async delegate
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        T[] arr = pool.Rent(RentalSize);
                        if (ManipulateArray) Clear(arr);
                        if (Async) await Task.Yield();
                        if (ManipulateArray) IterateAll(arr);
                        pool.Return(arr);
                    }
                })));
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        [BenchmarkCategory("NoInterpreter")]
        public async Task MultipleSerial()
        {
            ArrayPool<T> pool = Pool;
            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < _nestedArrays.Length; j++)
                {
                    _nestedArrays[j] = pool.Rent(RentalSize);
                    if (ManipulateArray) Clear(_nestedArrays[j]);
                }

                if (Async) await Task.Yield();

                for (int j = _nestedArrays.Length - 1; j >= 0; j--)
                {
                    if (ManipulateArray) IterateAll(_nestedArrays[j]);
                    pool.Return(_nestedArrays[j]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public async Task ProducerConsumer()
        {
            ArrayPool<T> pool = Pool;
            Channel<T[]> buffers = Channel.CreateBounded<T[]>(1);

            Task consumer = Task.Run(async delegate
            {
                ChannelReader<T[]> reader = buffers.Reader;
                while (true)
                {
                    ValueTask<bool> read = reader.WaitToReadAsync();
                    if (!(Async ? await read : read.AsTask().Result))
                    {
                        break;
                    }

                    while (reader.TryRead(out T[] buffer))
                    {
                        if (ManipulateArray) IterateAll(buffer);
                        pool.Return(buffer);
                    }
                }
            });

            ChannelWriter<T[]> writer = buffers.Writer;
            for (int i = 0; i < Iterations; i++)
            {
                T[] buffer = pool.Rent(RentalSize);
                if (ManipulateArray) IterateAll(buffer);
                ValueTask write = writer.WriteAsync(buffer);
                if (Async)
                {
                    await write;
                }
                else
                {
                    write.AsTask().Wait();
                }
            }
            writer.Complete();

            await consumer;
        }
    }

    [BenchmarkCategory(Categories.Libraries)]
    [GenericTypeArguments(typeof(byte))] // value type
    [GenericTypeArguments(typeof(object))] // reference type
    public class NonStandardArrayPoolTests<T>
    {
        private readonly ArrayPool<T> _createdPool = ArrayPool<T>.Create();

        private const int Iterations = 100_000;

        [Params(64)]
        public int RentalSize;

        [Params(false, true)]
        public bool UseSharedPool { get; set; }

        public ArrayPool<T> Pool => UseSharedPool ? ArrayPool<T>.Shared : _createdPool;

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void RentNoReturn()
        {
            ArrayPool<T> pool = Pool;
            for (int i = 0; i < Iterations; i++)
            {
                pool.Rent(RentalSize);
            }
        }
    }
}
