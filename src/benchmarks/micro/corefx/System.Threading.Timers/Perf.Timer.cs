// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Threading.Tasks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class TimerPerfTest
    {
        private readonly Timer[] _timers = new Timer[1_000_000];
        private readonly Task[] _tasks = new Task[Environment.ProcessorCount];

        [Benchmark]
        public void ShortScheduleAndDispose() => new Timer(_ => { }, null, 50, -1).Dispose();

        [Benchmark]
        public void LongScheduleAndDispose() => new Timer(_ => { }, null, int.MaxValue, -1).Dispose();

        [Benchmark]
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

        [Benchmark]
        public void SynchronousContention()
        {
            Task[] tasks = _tasks;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 1_000_000; j++)
                    {
                        new Timer(delegate { }, null, int.MaxValue, -1).Dispose();
                    }
                });
            }
            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void AsynchronousContention()
        {
            Task[] tasks = _tasks;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < 1_000_000; j++)
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
    }
}
