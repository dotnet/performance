// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.RegularExpressions.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Regex_Cache
    {
        private const int MaxConcurrency = 4;
        private int _cacheSizeOld;
        private IReadOnlyDictionary<(int total, int unique), string[]> _patterns;

        [GlobalSetup]
        public void Setup()
        {
            _cacheSizeOld = Regex.CacheSize;
            Regex.CacheSize = 0; // clean up cache
            
            _patterns = new Dictionary<(int total, int unique), string[]>
            {
                { (400_000, 7), CreatePatterns(400_000, 7)},
                { (400_000, 1), CreatePatterns(400_000, 1)},
                { (40_000, 7), CreatePatterns(40_000, 7)},
                { (40_000, 1_600), CreatePatterns(40_000, 1_600)}
            };
        }

        [GlobalCleanup]
        public void Cleanup() => Regex.CacheSize = _cacheSizeOld;

        [Benchmark]
        [Arguments(400_000, 7, 15)]         // default size, most common
        [Arguments(400_000, 1, 15)]         // default size, to test MRU
        [Arguments(40_000, 7, 0)]          // cache turned off
        [Arguments(40_000, 1_600, 15)]    // default size, to compare when cache used
        [Arguments(40_000, 1_600, 800)]    // larger size, to test cache is not O(n)
        [Arguments(40_000, 1_600, 3_200)]  // larger size, to test cache always hit
        [MemoryRandomization]
        public bool IsMatch(int total, int unique, int cacheSize)
        {
            if (Regex.CacheSize != cacheSize)
                Regex.CacheSize = cacheSize;
            
            string[] patterns = _patterns[(total, unique)];

            return RunTest(0, total, patterns);
        }

        private bool RunTest(int start, int total, string[] regexps)
        {
            bool isMatch = false;
            for (var i = 0; i < total; i++)
                isMatch ^= Regex.IsMatch("0123456789", regexps[start + i]);
            return isMatch;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [Arguments(400_000, 7, 15)]         // default size, most common
        [Arguments(400_000, 1, 15)]         // default size, to test MRU
        [Arguments(40_000, 7, 0)]          // cache turned off
        [Arguments(40_000, 1_600, 15)]    // default size, to compare when cache used
        [Arguments(40_000, 1_600, 800)]    // larger size, to test cache is not O(n)
        [Arguments(40_000, 1_600, 3_200)]  // larger size, to test cache always hit
        [MemoryRandomization]
        public async Task IsMatch_Multithreading(int total, int unique, int cacheSize)
        {
            if (Regex.CacheSize != cacheSize)
                Regex.CacheSize = cacheSize;
            
            string[] patterns = _patterns[(total, unique)];

            int sliceLength = total / MaxConcurrency;
            var tasks = new Task[MaxConcurrency];

            for (int i = 0; i < MaxConcurrency; i++)
            {
                int start = i * sliceLength;
                tasks[i] = Task.Run(() => RunTest(start, sliceLength, patterns));
            }

            await Task.WhenAll(tasks);
        }

        private static string[] CreatePatterns(int total, int unique)
        {
            var regexps = new string[total];
            // create: 
            {
                var i = 0;
                for (; i < unique; i++)
                {
                    // "(0+)" "(1+)" ..  "(9+)(9+)(8+)" ..
                    var sb = new StringBuilder();
                    foreach (var c in i.ToString())
                        sb.Append("(" + c + "+)");
                    regexps[i] = sb.ToString();
                }
                for (; i < total; i++) regexps[i] = regexps[i % unique];
            }

            // shuffle:
            const int someSeed = 101;  // const seed for reproducability
            var random = new Random(someSeed);
            for (var i = 0; i < total; i++)
            {
                var r = random.Next(i, total);
                var t = regexps[i];
                regexps[i] = regexps[r];
                regexps[r] = t;
            }

            return regexps;
        }
    }
}
