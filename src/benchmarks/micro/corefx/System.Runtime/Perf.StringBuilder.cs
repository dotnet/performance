// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public class Perf_StringBuilder
    {
        private readonly string _string0 = "";
        private readonly string _string100 = new string(Enumerable.Repeat('a', 100).ToArray());
        private readonly string _string200 = new string(Enumerable.Repeat('a', 200).ToArray());
        private readonly string _string1000 = new string(Enumerable.Repeat('a', 1000).ToArray());
        private StringBuilder _bigStringBuilder;

        [Benchmark]
        public void ctor() => new StringBuilder();

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        public void ctor_string(int length) => new StringBuilder(length == 100 ? _string100 : _string1000);

        [Benchmark]
        [Arguments(0)]
        [Arguments(200)]
        public void Append(int length)
        {
            string builtString = length == 0 ? _string0 : _string200;
            StringBuilder empty = new StringBuilder();

            for (int i = 0; i < 10000; i++)
                empty.Append(builtString); // Appends a string of length "length" to an increasingly large StringBuilder
        }

        public const int NUM_ITERS_CONCAT = 1000;
        public const int NUM_ITERS_APPEND = 1000;
        public const int NUM_ITERS_TOSTRING = 1000;

        public static string s1 = "12345";
        public static string s2 = "1234567890";
        public static string s3 = "1234567890abcde";
        public static string s4 = "1234567890abcdefghij";
        public static string s5 = "1234567890abcdefghijklmno";
        public static string s6 = "1234567890abcdefghijklmnopqrst";
        public static string s7 = "1234567890abcdefghijklmnopqrstuvwxy";
        public static string s8 = "1234567890abcdefghijklmnopqrstuvwxyzABCD";
        public static string s9 = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHI";
        public static string s10 = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN";

        [Benchmark]
        public string StringConcat()
        {
            string str = "";

            for (int j = 0; j < NUM_ITERS_CONCAT; j++)
                str += s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10;

            return str;
        }

        [Benchmark]
        public StringBuilder StringBuilderAppend()
        {
            StringBuilder sb = new StringBuilder();

            for (int j = 0; j < NUM_ITERS_APPEND; j++)
            {
                sb.Append(s1);
                sb.Append(s2);
                sb.Append(s3);
                sb.Append(s4);
                sb.Append(s5);
                sb.Append(s6);
                sb.Append(s7);
                sb.Append(s8);
                sb.Append(s9);
                sb.Append(s10);
            }

            return sb;
        }

        [Benchmark]
        public StringBuilder Append_ValueTypes()
        {
            var sb = new StringBuilder();

            for (int j = 0; j < NUM_ITERS_APPEND; j++)
            {
                sb.Append(sbyte.MaxValue);
                sb.Append(byte.MaxValue);
                sb.Append(short.MaxValue);
                sb.Append(ushort.MaxValue);
                sb.Append(int.MaxValue);
                sb.Append(uint.MaxValue);
                sb.Append(long.MaxValue);
                sb.Append(ulong.MaxValue);
                sb.Append(double.MaxValue);
                sb.Append(float.MaxValue);
                sb.Append(decimal.MaxValue);
                sb.Append(new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));
                sb.Append(new DateTime(2018, 12, 14));
                sb.Append(new DateTimeOffset(new DateTime(2018, 12, 14), default));
                sb.Append(new TimeSpan(1, 2, 3));
            }

            return sb;
        }

        [GlobalSetup(Target = nameof(StringBuilderToString))]
        public void SetupStringBuilderToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int j = 0; j < NUM_ITERS_TOSTRING; j++)
            {
                sb.Append(s1);
                sb.Append(s2);
                sb.Append(s3);
                sb.Append(s4);
                sb.Append(s5);
                sb.Append(s6);
                sb.Append(s7);
                sb.Append(s8);
                sb.Append(s9);
                sb.Append(s10);
            }

            _bigStringBuilder = sb;
        }

        [Benchmark]
        public string StringBuilderToString() => _bigStringBuilder.ToString();
    }
}
