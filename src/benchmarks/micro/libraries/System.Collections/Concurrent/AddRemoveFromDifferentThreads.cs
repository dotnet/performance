// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections.Concurrent
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections, Categories.NoWASM)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    [MinWarmupCount(6, forceAutoWarmup: true)]
    [MaxWarmupCount(10, forceAutoWarmup: true)]
    public class AddRemoveFromDifferentThreads<T>
    {
        const int NumThreads = 2;

        [Params(2_000_000)]
        public int Size;

        private Barrier _barrier;
        private Task _producer, _consumer;

        [IterationCleanup]
        public void IterationCleanup() => _barrier.Dispose();

        [IterationSetup(Target = nameof(ConcurrentBag))]
        public void SetupConcurrentBagIteration()
        {
            var bag = new ConcurrentBag<T>();

            _barrier = new Barrier(NumThreads + 1);
            
            _producer = Task.Factory.StartNew(() =>
            {
                _barrier.SignalAndWait();
                _barrier.SignalAndWait();

                for (int i = 0; i < Size; i++)
                {
                    bag.Add(default);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _consumer = Task.Factory.StartNew(() =>
            {
                _barrier.SignalAndWait();
                _barrier.SignalAndWait();

                int count = 0;
                while (count < Size)
                {
                    if (bag.TryTake(out T _))
                    {
                        count++;
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
            _barrier.SignalAndWait();
        }

        [Benchmark]
        public void ConcurrentBag() => SignalAndWaitForAllTasks();

        [IterationSetup(Target = nameof(ConcurrentStack))]
        public void SetupConcurrentStackIteration()
        {
            var stack = new ConcurrentStack<T>();

            _barrier = new Barrier(NumThreads + 1);
            
            _producer = Task.Factory.StartNew(() =>
            {
                _barrier.SignalAndWait();
                _barrier.SignalAndWait();

                for (int i = 0; i < Size; i++)
                {
                    stack.Push(default);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _consumer = Task.Factory.StartNew(() =>
            {
                _barrier.SignalAndWait();
                _barrier.SignalAndWait();

                int count = 0;
                while (count < Size)
                {
                    if (stack.TryPop(out T _))
                    {
                        count++;
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
            _barrier.SignalAndWait();
        }

        [Benchmark]
        public void ConcurrentStack() => SignalAndWaitForAllTasks();

        [IterationSetup(Target = nameof(ConcurrentQueue))]
        public void SetupConcurrentQueueIteration()
        {
            var queue = new ConcurrentQueue<T>();
            
            _barrier = new Barrier(NumThreads + 1);

            _producer = Task.Factory.StartNew(() =>
            {
                _barrier.SignalAndWait();
                _barrier.SignalAndWait();

                for (int i = 0; i < Size; i++)
                {
                    queue.Enqueue(default);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _consumer = Task.Factory.StartNew(() =>
            {
                _barrier.SignalAndWait();
                _barrier.SignalAndWait();

                int count = 0;
                while (count < Size)
                {
                    if (queue.TryDequeue(out T _))
                    {
                        count++;
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
            _barrier.SignalAndWait();
        }

        [Benchmark]
        public void ConcurrentQueue() => SignalAndWaitForAllTasks();

        private void SignalAndWaitForAllTasks()
        {
            _barrier.SignalAndWait();

            Task.WaitAll(_producer, _consumer);
        }
    }
}