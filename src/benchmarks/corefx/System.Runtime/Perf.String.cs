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
            yield return new StringArguments(10);
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

        private static readonly string[] s_getHashCodeStrings = new string[]
        {
            string.Empty,
            "  ",
            "TeSt!",
            "I think Turkish i \u0131s TROUBL\u0130NG",
            "dzsdzsDDZSDZSDZSddsz",
            "a\u0300\u00C0A\u0300A",
            "Foo\u0400Bar!",
            "a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a",
            "\u4e33\u4e65 Testing... \u4EE8",
        };
        
        public static IEnumerable<object> GetHashCodeArgs => s_getHashCodeStrings;
        
        [Benchmark]
        [ArgumentsSource(nameof(GetHashCodeArgs))]
        public int GetHashCode(string s)
            => s.GetHashCode();
        
        [Benchmark]
        [Arguments("", 0, " ")]
        [Arguments(" ", 1, "    ")]
        [Arguments("    ", 1, "Test")]
        [Arguments("Test", 2, " Test")]
        [Arguments(" Test", 2, "Test ")]
        [Arguments("Test ", 2, " Te st  ")]
        [Arguments(" Te st  ", 3,
            "\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005")]
        [Arguments(
            "\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005",
            3, " \u0400Te \u0400st")]
        [Arguments(" \u0400Te \u0400st", 3, " a\u0300\u00C0")]
        [Arguments(" a\u0300\u00C0", 3, " a \u0300 \u00C0 ")]
        [Arguments(" a \u0300 \u00C0 ", 4, "     ddsz dszdsz \t  dszdsz  \t        ")]
        [Arguments("     ddsz dszdsz \t  dszdsz  \t        ", 5, "")]
        public string Insert(string s1, int i, string s2)
            => s1.Insert(i, s2);

        [Benchmark]
        [Arguments(0)]
        [Arguments(1)]
        [Arguments(5)]
        [Arguments(18)]
        [Arguments(2142)]
        public string PadLeft(int n)
            => "a".PadLeft(n);

        [Benchmark]
        [Arguments("a", 0)]
        [Arguments("  ", 0)]
        [Arguments("  ", 1)]
        [Arguments("TeSt!", 0)]
        [Arguments("TeSt!", 2)]
        [Arguments("TeSt!", 3)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 0)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 18)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 22)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10)]
        [Arguments("a\u0300\u00C0A\u0300A", 0)]
        [Arguments("a\u0300\u00C0A\u0300A", 3)]
        [Arguments("a\u0300\u00C0A\u0300A", 4)]
        [Arguments("Foo\u0400Bar!", 0)]
        [Arguments("Foo\u0400Bar!", 3)]
        [Arguments("Foo\u0400Bar!", 4)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 0)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 3)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 0)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 4)]
        public string Remove_Int(string s, int i)
            => s.Remove(i);

        [Benchmark]
        [Arguments("a", 0, 0)]
        [Arguments("  ", 0, 1)]
        [Arguments("  ", 1, 0)]
        [Arguments("TeSt!", 0, 2)]
        [Arguments("TeSt!", 2, 1)]
        [Arguments("TeSt!", 3, 0)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 0, 3)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 18, 3)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 22, 1)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0, 8)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, 4)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10, 1)]
        [Arguments("a\u0300\u00C0A\u0300A", 0, 2)]
        [Arguments("a\u0300\u00C0A\u0300A", 3, 1)]
        [Arguments("a\u0300\u00C0A\u0300A", 4, 0)]
        [Arguments("Foo\u0400Bar!", 0, 4)]
        [Arguments("Foo\u0400Bar!", 3, 2)]
        [Arguments("Foo\u0400Bar!", 4, 1)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 0, 2)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 3, 3)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 0, 3)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 4, 1)]
        public string Remove_IntInt(string s, int i1, int i2)
            => s.Remove(i1, i2);

        [Benchmark]
        [Arguments("", 0)]
        [Arguments(" ", 0)]
        [Arguments(" ", 1)]
        [Arguments("TeSt", 0)]
        [Arguments("TeSt", 2)]
        [Arguments("TeSt", 3)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 0)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 18)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 22)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10)]
        [Arguments("a\u0300\u00C0A\u0300", 0)]
        [Arguments("a\u0300\u00C0A\u0300", 3)]
        [Arguments("a\u0300\u00C0A\u0300", 4)]
        [Arguments("Foo\u0400Bar", 0)]
        [Arguments("Foo\u0400Bar", 3)]
        [Arguments("Foo\u0400Bar", 4)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 0)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 3)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 0)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 4)]
        public string Substring_Int(string s, int i)
            => s.Substring(i);

        [Benchmark]
        [Arguments("", 0, 0)]
        [Arguments(" ", 0, 1)]
        [Arguments(" ", 1, 0)]
        [Arguments("TeSt", 0, 2)]
        [Arguments("TeSt", 2, 1)]
        [Arguments("TeSt", 3, 0)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 0, 3)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 18, 3)]
        [Arguments("I think Turkish i \u0131s TROUBL\u0130NG", 22, 1)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0, 8)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, 4)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10, 1)]
        [Arguments("a\u0300\u00C0A\u0300", 0, 2)]
        [Arguments("a\u0300\u00C0A\u0300", 3, 1)]
        [Arguments("a\u0300\u00C0A\u0300", 4, 0)]
        [Arguments("Foo\u0400Bar", 0, 4)]
        [Arguments("Foo\u0400Bar", 3, 2)]
        [Arguments("Foo\u0400Bar", 4, 1)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 0, 2)]
        [Arguments("a\u0020a\u00A0A\u2000a\u2001a\u2002A\u2003a\u2004a\u2005a", 3, 3)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 0, 3)]
        [Arguments("\u4e33\u4e65 Testing... \u4EE8", 4, 1)]
        public string Substring_IntInt(string s, int i1, int i2)
            => s.Substring(i1, i2);
        
        private static readonly object[] s_splitOptions = new object[]
        {
            StringSplitOptions.None,
            StringSplitOptions.RemoveEmptyEntries,
        };
        
        public static IEnumerable<object[]> SplitArgs => Permutations(s_trimStrings, s_trimCharArrays, s_splitOptions);

        [Benchmark]
        [ArgumentsSource(nameof(SplitArgs))]
        public string[] Split(string s, char[] arr, StringSplitOptions options)
            => s.Split(arr, options);
        
        private static IEnumerable<object[]> Permutations(params IEnumerable<object>[] values)
        {
            IEnumerator<object>[] enumerators = new IEnumerator<object>[values.Length];
            try
            {
                while (true)
                {
                    for (int i = 0; i < enumerators.Length; i++)
                    {
                        if (enumerators[i] != null)
                        {
                            if (enumerators[i].MoveNext())
                                break;

                            if (i == enumerators.Length - 1)
                                yield break;
                        }

                        enumerators[i] = values[i].GetEnumerator();
                        if (!enumerators[i].MoveNext())
                            throw new ArgumentException("all arguments must have one or more elements", "values");
                    }

                    object[] result = new object[values.Length];

                    for (int i = 0; i < enumerators.Length; i++)
                        result[i] = enumerators[i].Current;

                    yield return result;
                }
            }
            finally
            {
                foreach (var enumerator in enumerators)
                    if (enumerator != null)
                        enumerator.Dispose();
            }
        }
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