// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.CoreCLR)]
    public class Perf_Interlocked
    {
        private const int IterationCount = 10_000_000;

        [Benchmark]
        public void Increment_int()
        {
            int location = 0;

            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Increment(ref location);
            }
        }

        [Benchmark]
        public void Decrement_int()
        {
            int location = 0;
            
            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Decrement(ref location);
            }
        }

        [Benchmark]
        public void Increment_long()
        {
            long location = 0;
            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Increment(ref location);
            }
        }

        [Benchmark]
        public void Decrement_long()
        {
            long location = 0;

            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Decrement(ref location);
            }
        }

        [Benchmark]
        public void Add_int()
        {
            int location = 0;

            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Add(ref location, 2);
            }
        }

        [Benchmark]
        public void Add_long()
        {
            long location = 0;

            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Add(ref location, 2);
            }
        }

        [Benchmark]
        public void Exchange_int()
        {
            int location = 0;
            int newValue = 1;
            
            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Exchange(ref location, newValue);
            }
        }

        [Benchmark]
        public void Exchange_long()
        {
            long location = 0;
            long newValue = 1;
            
            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.Exchange(ref location, newValue);
            }
        }

        [Benchmark]
        public void CompareExchange_int()
        {
            int location = 0;
            int newValue = 1;
            int comparand = 0;
            
            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.CompareExchange(ref location, newValue, comparand);
            }
        }

        [Benchmark]
        public void CompareExchange_long()
        {
            long location = 0;
            long newValue = 1;
            long comparand = 0;
            
            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.CompareExchange(ref location, newValue, comparand);
            }
        }

        [Benchmark]
        public void CompareExchange_object_Match()
        {
            string location = "What?";
            string newValue = "World";
            string comparand = "What?";

            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.CompareExchange(ref location, newValue, comparand);
            }
        }

        [Benchmark]
        public void CompareExchange_object_NoMatch()
        {
            string location = "Hello";
            string newValue = "World";
            string comparand = "What?";

            for (int i = 0; i < IterationCount; i++)
            {
                Interlocked.CompareExchange(ref location, newValue, comparand);
            }
        }
    }
}
