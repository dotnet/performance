// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MicroBenchmarks;

namespace Microsoft.Extensions.Primitives
{
    [BenchmarkCategory(Categories.Libraries)]
    public class StringSegmentBenchmark
    {
        private readonly StringSegment _segment = new StringSegment("Hello world!");
        private readonly StringSegment _segment2 = new StringSegment("Hello world!".AsSpan().ToString()); // different backing string instance
        private readonly StringSegment _largeSegment = new StringSegment("Hello, World!, Hello people! My Car Is Cool. Your Carport is blue.");
        private readonly StringSegment _trimSegment = new StringSegment("   Hello world!    ");
        private readonly object _boxedSegment;
        private readonly char[] _indexOfAnyChars = { 'w', 'l' };

        public StringSegmentBenchmark()
        {
            _boxedSegment = _segment;
        }

        [Benchmark]
        public StringSegment Ctor_String() => new StringSegment("Hello world!");

        [Benchmark]
        public string GetValue() => _segment.Value;

        [Benchmark]
        public char Indexer()
        {
            var segment = _segment;
            char result = default;
            for (int i = 0; i < segment.Length; i++)
            {
                result ^= segment[i];
            }
            return result;
        }

        [Benchmark]
        public bool Equals_Object_Invalid() => _segment.Equals(null as object);

        [Benchmark]
        [MemoryRandomization]
        public bool Equals_Object_Valid() => _segment2.Equals(_boxedSegment);

        [Benchmark]
        [MemoryRandomization]
        public bool Equals_Valid() => _segment2.Equals(_segment);

        [Benchmark]
        public bool Equals_String() => _segment2.Equals("Hello world!");

        [Benchmark]
        public int GetSegmentHashCode() => _segment.GetHashCode();

        [Benchmark]
        public bool StartsWith() => _largeSegment.StartsWith("Hel", StringComparison.Ordinal);

        [Benchmark]
        public bool EndsWith() => _largeSegment.EndsWith("ld!", StringComparison.Ordinal);

        [Benchmark]
        public string SubString() => _segment.Substring(3, 2);

        [Benchmark]
        public StringSegment SubSegment() => _segment.Subsegment(3, 2);

        [Benchmark]
        public int IndexOf() => _largeSegment.IndexOf(' ', 1, 7);

        [Benchmark]
        public int IndexOfAny() => _largeSegment.IndexOfAny(_indexOfAnyChars, 1, 7);

        [Benchmark]
        public int LastIndexOf() => _largeSegment.LastIndexOf('l');

        [Benchmark]
        public StringSegment Trim() => _trimSegment.Trim();

        [Benchmark]
        public StringSegment TrimStart() => _trimSegment.TrimStart();

        [Benchmark]
        public StringSegment TrimEnd() => _trimSegment.TrimEnd();
    }
}
