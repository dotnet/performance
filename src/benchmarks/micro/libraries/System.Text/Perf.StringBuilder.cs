// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class Perf_StringBuilder
    {
        const int LOHAllocatedStringSize = 100_000;

        private string _stringLOH;
        private string _string100;
        private StringBuilder _builderSingleSegment100;
        private StringBuilder _builderSingleSegmentLOH;
        private StringBuilder _builderMultipleSegments100;
        private StringBuilder _builderMultipleSegmentsLOH;

        [GlobalSetup(Targets = new[] { nameof(ctor_string), nameof(Append_Memory) })]
        public void Setup_ctor_string()
        {
            _stringLOH = new string('a', LOHAllocatedStringSize);
            _string100 = new string('a', 100);
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(LOHAllocatedStringSize)]
        [MemoryRandomization]
        public StringBuilder ctor_string(int length) => new StringBuilder(length == 100 ? _string100 : _stringLOH);

        [Benchmark]
        [Arguments(100)]
        [Arguments(LOHAllocatedStringSize)]
        public StringBuilder ctor_capacity(int length) => new StringBuilder(length);

        [GlobalSetup(Target = nameof(ToString_SingleSegment))]
        public void Setup_ToString_SingleSegment()
        {
            _builderSingleSegment100 = new StringBuilder(new string('a', 100));
            _builderSingleSegmentLOH = new StringBuilder(new string('a', LOHAllocatedStringSize));
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(LOHAllocatedStringSize)]
        public string ToString_SingleSegment(int length) => (length == 100 ? _builderSingleSegment100 : _builderSingleSegmentLOH).ToString();

        [GlobalSetup(Target = nameof(ToString_MultipleSegments))]
        public void Setup_ToString_MultipleSegments()
        {
            _builderMultipleSegments100 = Append_Char(100); // 16 + 32 + 48 + 96 char segments
            _builderMultipleSegmentsLOH = Append_Char(LOHAllocatedStringSize);
        }

        // internally StringBuilder is a linked list of StringBuilders and each of them contains a buffer of character (char[])
        // this benchmark tests this very common execution path - joining all the buffers from multiple StringBuffer instances into one string
        [Benchmark]
        [Arguments(100)]
        [Arguments(LOHAllocatedStringSize)]
        public string ToString_MultipleSegments(int length) => (length == 100 ? _builderMultipleSegments100 : _builderMultipleSegmentsLOH).ToString();

        [Benchmark]
        [Arguments(100)]
        [Arguments(LOHAllocatedStringSize)]
        [MemoryRandomization]
        public StringBuilder Append_Char(int length)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                builder.Append('a');
            }

            return builder;
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(LOHAllocatedStringSize)]
        public StringBuilder Append_Char_Capacity(int length)
        {
            StringBuilder builder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                builder.Append('a');
            }

            return builder;
        }

        [Benchmark]
        [Arguments(1)]
        [Arguments(1_000)]
        public StringBuilder Append_Strings(int repeat)
        {
            StringBuilder builder = new StringBuilder();

            // strings are not sorted by length to mimic real input
            for (int i = 0; i < repeat; i++)
            {
                builder.Append("12345");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxy");
                builder.Append("1234567890");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHI");
                builder.Append("1234567890abcde");
                builder.Append("1234567890abcdefghijklmnopqrstuvwxyzABCD");
                builder.Append("1234567890abcdefghijklmnopqrst");
                builder.Append("1234567890abcdefghij");
                builder.Append("1234567890abcdefghijklmno");
            }

            return builder;
        }

        [Benchmark]
        public StringBuilder AppendLine_Strings()
        {
            StringBuilder builder = new StringBuilder();

            // strings are not sorted by length to mimic real input
            builder.AppendLine("12345");
            builder.AppendLine("1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN");
            builder.AppendLine("1234567890abcdefghijklmnopqrstuvwxy");
            builder.AppendLine("1234567890");
            builder.AppendLine("1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHI");
            builder.AppendLine("1234567890abcde");
            builder.AppendLine("1234567890abcdefghijklmnopqrstuvwxyzABCD");
            builder.AppendLine("1234567890abcdefghijklmnopqrst");
            builder.AppendLine("1234567890abcdefghij");
            builder.AppendLine("1234567890abcdefghijklmno");

            return builder;
        }

        [Benchmark]
        public StringBuilder Append_Primitives()
        {
            var builder = new StringBuilder();

            builder.Append(true);
            builder.Append(sbyte.MaxValue);
            builder.Append(byte.MaxValue);
            builder.Append(short.MaxValue);
            builder.Append(ushort.MaxValue);
            builder.Append(int.MaxValue);
            builder.Append(uint.MaxValue);
            builder.Append(long.MaxValue);
            builder.Append(ulong.MaxValue);
            builder.Append(double.MaxValue);
            builder.Append(float.MaxValue);
            builder.Append(decimal.MaxValue);

            return builder;
        }

        // as of today the following types are added using Append(object) and hence boxed
        [Benchmark]
        public StringBuilder Append_ValueTypes()
        {
            var builder = new StringBuilder();

            var dateTime = new DateTime(2018, 12, 14);
            var timeSpan = new TimeSpan(1, 2, 0);
            var dateTimeOffset = new DateTimeOffset(dateTime, timeSpan);
            var guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);

            // to amortize the cost of creating the value types we add them few times
            builder.Append(guid); builder.Append(dateTime); builder.Append(timeSpan); builder.Append(dateTimeOffset);
            builder.Append(guid); builder.Append(dateTime); builder.Append(timeSpan); builder.Append(dateTimeOffset);
            builder.Append(guid); builder.Append(dateTime); builder.Append(timeSpan); builder.Append(dateTimeOffset);
            builder.Append(guid); builder.Append(dateTime); builder.Append(timeSpan); builder.Append(dateTimeOffset);

            return builder;
        }

        // on .NET 6+, interpolated string handlers make appending more efficient by avoiding boxing and using ISpanFormattable.
        [Benchmark]
        // on .NET 6+, interpolated string handlers make appending more efficient by avoiding boxing and using ISpanFormattable.
        [MemoryRandomization]
        public StringBuilder Append_ValueTypes_Interpolated()
        {
            var builder = new StringBuilder();

            var dateTime = new DateTime(2018, 12, 14);
            var timeSpan = new TimeSpan(1, 2, 0);
            var dateTimeOffset = new DateTimeOffset(dateTime, timeSpan);
            var guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);

            // to amortize the cost of creating the value types we add them few times
            for (int i = 1; i <= 4; i++)
            {
                builder.Append($"{guid} {dateTime} {timeSpan} {dateTimeOffset}");
            }

            return builder;
        }

        [Benchmark]
        public StringBuilder Append_Memory()
        {
            ReadOnlyMemory<char> memory = _string100.AsMemory();
            StringBuilder builder = new StringBuilder();

            builder.Append(memory); builder.Append(memory); builder.Append(memory); builder.Append(memory);
            builder.Append(memory); builder.Append(memory); builder.Append(memory); builder.Append(memory);
            builder.Append(memory); builder.Append(memory); builder.Append(memory); builder.Append(memory);
            builder.Append(memory); builder.Append(memory); builder.Append(memory); builder.Append(memory);

            return builder;
        }

#if !NETFRAMEWORK
        [GlobalSetup(Target = nameof(Append_NonEmptySpan))]
        public void Setup_Append_NonEmptySpan() => _string100 = new string('a', 100);

        [Benchmark]
        public StringBuilder Append_NonEmptySpan()
        {
            ReadOnlySpan<char> span = _string100.AsSpan();
            StringBuilder builder = new StringBuilder();

            builder.Append(span); builder.Append(span); builder.Append(span); builder.Append(span);
            builder.Append(span); builder.Append(span); builder.Append(span); builder.Append(span);
            builder.Append(span); builder.Append(span); builder.Append(span); builder.Append(span);
            builder.Append(span); builder.Append(span); builder.Append(span); builder.Append(span);

            return builder;
        }
#endif

        [Benchmark]
        [MemoryRandomization]
        public StringBuilder Insert_Primitives()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 1; i <= 10; i++)
            {
                builder.Insert(builder.Length / i, true);
                builder.Insert(builder.Length / i, sbyte.MaxValue);
                builder.Insert(builder.Length / i, byte.MaxValue);
                builder.Insert(builder.Length / i, short.MaxValue);
                builder.Insert(builder.Length / i, ushort.MaxValue);
                builder.Insert(builder.Length / i, int.MaxValue);
                builder.Insert(builder.Length / i, uint.MaxValue);
                builder.Insert(builder.Length / i, long.MaxValue);
                builder.Insert(builder.Length / i, ulong.MaxValue);
                builder.Insert(builder.Length / i, double.MaxValue);
                builder.Insert(builder.Length / i, float.MaxValue);
                builder.Insert(builder.Length / i, decimal.MaxValue);
            }

            return builder;
        }

        [Benchmark]
        [MemoryRandomization]
        public StringBuilder Insert_Strings()
        {
            StringBuilder builder = new StringBuilder();

            builder.Insert(builder.Length / 1, "12345");
            builder.Insert(builder.Length / 2, "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN");
            builder.Insert(builder.Length / 3, "1234567890abcdefghijklmnopqrstuvwxy");
            builder.Insert(builder.Length / 4, "1234567890");
            builder.Insert(builder.Length / 5, "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHI");
            builder.Insert(builder.Length / 6, "1234567890abcde");
            builder.Insert(builder.Length / 7, "1234567890abcdefghijklmnopqrstuvwxyzABCD");
            builder.Insert(builder.Length / 8, "1234567890abcdefghijklmnopqrst");
            builder.Insert(builder.Length / 9, "1234567890abcdefghij");
            builder.Insert(builder.Length / 10, "1234567890abcdefghijklmno");

            return builder;
        }
    }
}
