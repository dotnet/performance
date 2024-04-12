// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Threading.Tasks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Timer
    {
        private const int Count = 1_000_000;

        private Timer[] _timers;
        private Task[] _tasks;

        [Benchmark]
        [MemoryRandomization]
        public void ShortScheduleAndDispose() => new Timer(_ => { }, null, 50, -1).Dispose();

        [Benchmark]
        [MemoryRandomization]
        public void LongScheduleAndDispose() => new Timer(_ => { }, null, int.MaxValue, -1).Dispose();

        [GlobalSetup(Target = nameof(ScheduleManyThenDisposeMany))]
        public void SetupScheduleManyThenDisposeMany() => _timers = new Timer[Count];

        [Benchmark]
        [MemoryRandomization]
        public void ScheduleManyThenDisposeMany()
        {
            Timer[] timers = _timers;

            for (int i = 0; i < timers.Length; i++)
            {
                timers[i] = new Timer(_ => { }, null, int.MaxValue, -1);
            }

            foreach (Timer timer in timers)
            {
                timer.Dispose();
            }
        }

        [GlobalSetup(Targets = new[] { nameof(SynchronousContention), nameof(AsynchronousContention) })]
        public void SetupTasks() => _tasks = new Task[Environment.ProcessorCount];

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public void SynchronousContention()
        {
            Task[] tasks = _tasks;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < Count; j++)
                    {
                        new Timer(delegate { }, null, int.MaxValue, -1).Dispose();
                    }
                });
            }
            Task.WaitAll(tasks);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public void AsynchronousContention()
        {
            Task[] tasks = _tasks;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < Count; j++)
                    {
                        using (var t = new Timer(delegate { }, null, int.MaxValue, -1))
                        {
                            await Task.Yield();
                        }
                    }
                });
            }
            Task.WaitAll(tasks);
        }

        [GlobalSetup(Target = nameof(ShortScheduleAndDisposeWithFiringTimers))]
        public void SetupShortScheduleAndDisposeWithFiringTimers()
        {
            _timers = new Timer[Count];
            for (int i = 0; i < _timers.Length; i++)
            {
                _timers[i] = new Timer(_ => { }, null, i, i);
            }
            Thread.Sleep(1000);
        }

        [Benchmark]
        [MemoryRandomization]
        public void ShortScheduleAndDisposeWithFiringTimers() => new Timer(_ => { }, 0, 100, 100).Dispose();

        [GlobalCleanup(Target = nameof(ShortScheduleAndDisposeWithFiringTimers))]
        public void CleanupShortScheduleAndDisposeWithFiringTimers()
        {
            using (var are = new AutoResetEvent(false))
            {
                foreach (Timer t in _timers)
                {
                    t.Dispose(are);
                    are.WaitOne();
                }
            }
        }
    }
}
