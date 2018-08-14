// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Tests
{
    public partial class Perf_String
    {
        public static IEnumerable<object> TestStringSizes()
        {
            yield return new StringArguments(100);
            yield return new StringArguments(1000);
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public char[] GetChars(StringArguments size) // the argument is called "size" to keep the old benchmark ID, do NOT rename it
            => size.TestString1.ToCharArray();
        
        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public string Concat_str_str(StringArguments size)
            => string.Concat(size.TestString1, size.TestString2);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public string Concat_str_str_str(StringArguments size)
            => string.Concat(size.TestString1, size.TestString2, size.TestString3);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public string Concat_str_str_str_str(StringArguments size)
            => string.Concat(size.TestString1, size.TestString2, size.TestString3, size.TestString4);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public bool Contains(StringArguments size)
            => size.TestString1.Contains(size.Q3);

        [Benchmark]
        [ArgumentsSource(nameof(TestStringSizes))]
        public bool StartsWith(StringArguments size)
            => size.TestString1.StartsWith(size.Q1);

        [Benchmark]
        [Arguments("")]
        [Arguments("TeSt!")]
        [Arguments("dzsdzsDDZSDZSDZSddsz")]
        public int GetHashCode(string s)
            => s.GetHashCode();
        
        [Benchmark]
        [Arguments("Test", 2, " Test")]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, "Test")]
        public string Insert(string s1, int i, string s2)
            => s1.Insert(i, s2);

        [Benchmark]
        [Arguments(18)]
        [Arguments(2142)]
        public string PadLeft(int n)
            => "a".PadLeft(n);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10)]
        public string Remove_Int(string s, int i)
            => s.Remove(i);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0, 8)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, 4)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10, 1)]
        public string Remove_IntInt(string s, int i1, int i2)
            => s.Remove(i1, i2);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10)]
        public string Substring_Int(string s, int i)
            => s.Substring(i);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0, 8)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, 4)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10, 1)]
        public string Substring_IntInt(string s, int i1, int i2)
            => s.Substring(i1, i2);
        
        [Benchmark]
        [Arguments("A B C D E F G H I J K L M N O P Q R S T U V W X Y Z", new char[] { ' ' }, StringSplitOptions.None)]
        [Arguments("A B C D E F G H I J K L M N O P Q R S T U V W X Y Z", new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)]
        [Arguments("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new char[]{' '}, StringSplitOptions.None)]
        [Arguments("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new char[]{' '}, StringSplitOptions.RemoveEmptyEntries)]
        public string[] Split(string s, char[] arr, StringSplitOptions options)
            => s.Split(arr, options);
    }

    public class StringArguments
    {
        public int Size { get; }

        public string TestString1 { get; }
        public string TestString2 { get; }
        public string TestString3 { get; }
        public string TestString4 { get; }

        public string Q1 { get; }
        public string Q3 { get; }

        public override string ToString() => Size.ToString(); // this argument replaced an int argument called size

        public StringArguments(int size)
        {
            Size = size;

            PerfUtils utils = new PerfUtils();
            TestString1 = utils.CreateString(size);
            TestString2 = utils.CreateString(size);
            TestString3 = utils.CreateString(size);
            TestString4 = utils.CreateString(size);

            Q1 = TestString1.Substring(0, TestString1.Length / 4);
            Q3 = TestString1.Substring(TestString1.Length / 2, TestString1.Length / 4);
        }
    }
}