// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Enum
    {
        [Flags]
        public enum Colors
        {
            Red = 0x1,
            Orange = 0x2,
            Yellow = 0x4,
            Green = 0x8,
            Blue = 0x10
        }

        public enum ByteEnum : byte
        {
            A,
            B
        }

        [Benchmark]
        [Arguments(Colors.Yellow)]
        [Arguments(Colors.Yellow | Colors.Blue)]
        [Arguments(Colors.Red | Colors.Orange | Colors.Yellow | Colors.Green | Colors.Blue)]
        [Arguments(Colors.Yellow | (Colors)0x20)]
        [Arguments(0x20)]
        [MemoryRandomization]
        public string ToString_Flags(Colors value) => value.ToString();

        [Benchmark]
        [Arguments(SearchOption.TopDirectoryOnly)]
        [Arguments(SearchOption.AllDirectories)]
        [Arguments((SearchOption)(-1))]
        public string ToString_NonFlags_Small(SearchOption value) => value.ToString();

        [Benchmark]
        [Arguments(UnicodeCategory.UppercaseLetter)]
        [Arguments(UnicodeCategory.Control)]
        [Arguments(UnicodeCategory.Format)]
        [Arguments(UnicodeCategory.OtherNotAssigned)]
        [Arguments((UnicodeCategory)42)]
        [MemoryRandomization]
        public string ToString_NonFlags_Large(UnicodeCategory value) => value.ToString();

        [Benchmark]
        [Arguments(DayOfWeek.Sunday, "")]
        [Arguments(DayOfWeek.Monday, "g")]
        [Arguments(DayOfWeek.Tuesday, "d")]
        [Arguments(DayOfWeek.Wednesday, "x")]
        [Arguments(DayOfWeek.Thursday, "f")]
        [Arguments(DayOfWeek.Friday, "X")]
        [Arguments(DayOfWeek.Saturday, "D")]
        [Arguments((DayOfWeek)7, "G")]
        [Arguments((DayOfWeek)8, "F")]
        [MemoryRandomization]
        public string ToString_Format_NonFlags(DayOfWeek value, string format) => value.ToString(format);

        [Benchmark]
        [Arguments(AttributeTargets.All, "")]
        [Arguments(AttributeTargets.All, "g")]
        [Arguments(AttributeTargets.All, "d")]
        [Arguments(AttributeTargets.All, "x")]
        [Arguments(AttributeTargets.All, "f")]
        [MemoryRandomization]
        public string ToString_Format_Flags_Large(AttributeTargets value, string format) => value.ToString(format);

        [Benchmark]
        [Arguments("Red")]
        [Arguments("Red, Orange, Yellow, Green, Blue")]
        public Colors Parse_Flags(string text) => (Colors)Enum.Parse(typeof(Colors), text);

        [Benchmark]
        [Arguments("Red")]
        [Arguments("Red, Orange, Yellow, Green, Blue")]
        [MemoryRandomization]
        public bool TryParseGeneric_Flags(string text) => Enum.TryParse<Colors>(text, out _);

        private Colors _greenAndRed = Colors.Green | Colors.Red;

        [Benchmark]
        public bool HasFlag() => _greenAndRed.HasFlag(Colors.Green);

        private ByteEnum _byteEnum = ByteEnum.A;

        [Benchmark]
        public int Compare() => Comparer<ByteEnum>.Default.Compare(_byteEnum, _byteEnum);

        [Benchmark]
        public string GetName_NonGeneric_Flags() => Enum.GetName(typeof(Colors), Colors.Blue);

        [Benchmark]
        [Arguments(Colors.Red)]
        [Arguments(Colors.Red | Colors.Green)]
        [Arguments(0x20)]
        [MemoryRandomization]
        public string StringFormat(Colors value) =>
            string.Format("{0} {0:d} {0:f} {0:g} {0:x}", value);

        [Benchmark]
        [Arguments(Colors.Red)]
        [Arguments(Colors.Red | Colors.Green)]
        [Arguments(0x20)]
        public string InterpolateIntoString(Colors value) =>
            $"{value} {value:d} {value:f} {value:g} {value:x}";

#if NET5_0_OR_GREATER
        private Colors _colorValue = Colors.Blue;
        private DayOfWeek _dayOfWeekValue = DayOfWeek.Saturday;

        [Benchmark]
        public bool IsDefined_Generic_Flags() => Enum.IsDefined(_colorValue);

        [Benchmark]
        public bool IsDefined_Generic_NonFlags() => Enum.IsDefined(_dayOfWeekValue);

        [Benchmark]
        public string GetName_Generic_Flags() => Enum.GetName(_colorValue);

        [Benchmark]
        public string GetName_Generic_NonFlags() => Enum.GetName(_dayOfWeekValue);

        [Benchmark]
        public string[] GetNames_Generic() => Enum.GetNames<Colors>();

        [Benchmark]
        public Colors[] GetValues_Generic() => Enum.GetValues<Colors>();
#endif

#if NET6_0_OR_GREATER
        // TODO: https://github.com/dotnet/performance/issues/2776
        // These tests use manual invocations of the relevant APIs rather than using
        // language syntax for string interpolation as doing so would require C# 10
        // and the repo is currently pinned to C# 9.  Once that limit is lifted, these
        // tests should switch to testing the language syntax.

        private static readonly char[] s_scratch = new char[512];

        [Benchmark]
        [Arguments(Colors.Red)]
        [Arguments(Colors.Red | Colors.Green)]
        [Arguments(0x20)]
        public bool InterpolateIntoSpan_Flags(Colors value) => InterpolateIntoSpan(s_scratch, value);
        
        [Benchmark]
        [Arguments(DayOfWeek.Saturday)]
        [Arguments(42)]
        public bool InterpolateIntoSpan_NonFlags(Colors value) => InterpolateIntoSpan(s_scratch, value);

        private static bool InterpolateIntoSpan<T>(Span<char> span, T value)
        {
            // span.TryWrite($"{value} {value:d} {value:f} {value:g} {value:x}")
            var handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(4, 5, span, out bool shouldAppend);
            return
                shouldAppend &&
                handler.AppendFormatted(value) &&
                handler.AppendLiteral(" ") &&
                handler.AppendFormatted(value, "d") &&
                handler.AppendLiteral(" ") &&
                handler.AppendFormatted(value, "f") &&
                handler.AppendLiteral(" ") &&
                handler.AppendFormatted(value, "g") &&
                handler.AppendLiteral(" ") &&
                handler.AppendFormatted(value, "x") &&
                MemoryExtensions.TryWrite(span, ref handler, out _);
        }

        private static StringBuilder s_sb = new StringBuilder();

        [Benchmark]
        [Arguments(Colors.Red)]
        [Arguments(Colors.Red | Colors.Green)]
        [Arguments(0x20)]
        public void InterpolateIntoStringBuilder_Flags(Colors value) => InterpolateIntoStringBuilder(s_sb, value);

        [Benchmark]
        [Arguments(DayOfWeek.Saturday)]
        [Arguments(42)]
        public void InterpolateIntoStringBuilder_NonFlags(Colors value) => InterpolateIntoStringBuilder(s_sb, value);

        private static void InterpolateIntoStringBuilder<T>(StringBuilder sb, T value)
        {
            sb.Clear();

            // sb.Append($"{value} {value:d} {value:f} {value:g} {value:x}")
            var handler = new StringBuilder.AppendInterpolatedStringHandler(4, 5, sb, null);
            handler.AppendFormatted(value);
            handler.AppendLiteral(" ");
            handler.AppendFormatted(value, "d");
            handler.AppendLiteral(" ");
            handler.AppendFormatted(value, "f");
            handler.AppendLiteral(" ");
            handler.AppendFormatted(value, "g");
            handler.AppendLiteral(" ");
            handler.AppendFormatted(value, "x");
            sb.Append(ref handler);
        }
#endif

#if NET7_0_OR_GREATER
        [Benchmark]
        public Array GetValuesAsUnderlyingType_Generic() => Enum.GetValuesAsUnderlyingType<UnicodeCategory>();

        [Benchmark]
        public Array GetValuesAsUnderlyingType_NonGeneric() => Enum.GetValuesAsUnderlyingType(typeof(UnicodeCategory));
#endif
    }
}