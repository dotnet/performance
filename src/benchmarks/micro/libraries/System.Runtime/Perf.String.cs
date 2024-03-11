// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
    public class Perf_String
    {
        // the culture-specific methods are tested in Perf_StringCultureSpecific class

        public static IEnumerable<object> TestStringSizes()
        {
            yield return new StringArguments(100);
            yield return new StringArguments(1000);
        }

        [Benchmark]
        [Arguments(1)]
        [Arguments(10)]
        [Arguments(100)]
        public string CtorCharCount(int size) => new string('a', size);

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

        private static readonly IEnumerable<char> s_longCharEnumerable = Enumerable.Range(0, 1000).Select(i => (char)('a' + i % 26));

        [Benchmark]
        [MemoryRandomization]
        public string Concat_CharEnumerable() =>
            string.Concat(s_longCharEnumerable);

        private static string[] s_stringArray = new[] { "hello", "world", "how", "are", "you", "today" };
        private static List<string> s_stringList = new List<string>(s_stringArray);
        private static IEnumerable<string> s_stringEnumerable = s_stringArray.Select(s => s.ToUpperInvariant());

        [Benchmark]
        public string Join_Array() => string.Join(", ", s_stringArray);

        [Benchmark]
        public string Join_List() => string.Join(", ", s_stringList);

        [Benchmark]
        [MemoryRandomization]
        public string Join_Enumerable() => string.Join(", ", s_stringEnumerable);

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
        [MemoryRandomization]
        public string Remove_Int(string s, int i)
            => s.Remove(i);

        [Benchmark]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 0, 8)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 7, 4)]
        [Arguments("dzsdzsDDZSDZSDZSddsz", 10, 1)]
        [MemoryRandomization]
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
        [MemoryRandomization]
        public string Substring_IntInt(string s, int i1, int i2)
            => s.Substring(i1, i2);
        
        [Benchmark]
        [Arguments("A B C D E F G H I J K L M N O P Q R S T U V W X Y Z", new char[] { ' ' }, StringSplitOptions.None)]
        [Arguments("A B C D E F G H I J K L M N O P Q R S T U V W X Y Z", new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)]
        [Arguments("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new char[]{' '}, StringSplitOptions.None)]
        [Arguments("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new char[]{' '}, StringSplitOptions.RemoveEmptyEntries)]
        [MemoryRandomization]
        public string[] Split(string s, char[] arr, StringSplitOptions options)
            => s.Split(arr, options);

        [Benchmark]
        [Arguments("Test")]
        [Arguments(" Test")]
        [Arguments("Test ")]
        [Arguments(" Te st  ")]
        [MemoryRandomization]
        public string Trim(string s)
            => s.Trim();

        [Benchmark]
        [Arguments("Test")]
        [Arguments(" Test")]
        [MemoryRandomization]
        public string TrimStart(string s)
            => s.TrimStart();

        [Benchmark]
        [Arguments("Test")]
        [Arguments("Test ")]
        public string TrimEnd(string s)
            => s.TrimEnd();
        
        [Benchmark]
        [Arguments("Test", new [] {' ', (char) 8197})]
        [Arguments(" Test", new [] {' ', (char) 8197})]
        [Arguments("Test ", new [] {' ', (char) 8197})]
        [Arguments(" Te st  ", new [] {' ', (char) 8197})]
        [MemoryRandomization]
        public string Trim_CharArr(string s, char[] c)
            => s.Trim(c);

        [Benchmark]
        [Arguments("Test", new [] {' ', (char) 8197})]
        [Arguments(" Test", new [] {' ', (char) 8197})]
        [MemoryRandomization]
        public string TrimStart_CharArr(string s, char[] c)
            => s.TrimStart(c);

        [Benchmark]
        [Arguments("Test", new [] {' ', (char) 8197})]
        [Arguments("Test ", new [] {' ', (char) 8197})]
        [MemoryRandomization]
        public string TrimEnd_CharArr(string s, char[] c)
            => s.TrimEnd(c);

        [Benchmark]
        [ArgumentsSource(nameof(ReplaceArguments))]
        [MemoryRandomization]
        public string Replace_Char(string text, char oldChar, char newChar)
            => text.Replace(oldChar, newChar);

        public static IEnumerable<object[]> ReplaceArguments()
        {
            yield return new object[] { "Hello", 'l', '!' };    // Contains two 'l'
            yield return new object[] { "Hello", 'a', 'b' };    // Contains one 'a'
            yield return new object[] { "This is a very nice sentence", 'z', 'y' }; // 'z' does not exist in the string
            yield return new object[] { "This is a very nice sentence", 'i', 'I' }; // 'i' occurs 3 times in the string
            yield return new object[] { PerfUtils.CreateRandomString(100, seed: 42), 'b', '+' };    // b occurs 8 times in the string
            yield return new object[] { PerfUtils.CreateRandomString(1000, seed: 42), 'b', '+' };   // b occurs 42 times in the string
        }

        [Benchmark]
        [Arguments("This is a very nice sentence", "bad", "nice")] // there are no "bad" words in the string
        [Arguments("This is a very nice sentence", "nice", "bad")] // there are is one "nice" word in the string
        [Arguments("This is a very nice sentence. This is another very nice sentence.", "a", "b")] // both strings are single characters
        [Arguments("This is a very nice sentence. This is another very nice sentence.", "a", "")] // old string is a single character
        [MemoryRandomization]
        public string Replace_String(string text, string oldValue, string newValue)
            => text.Replace(oldValue, newValue);

        private static readonly char[] s_colonAndSemicolon = { ':', ';' };

        [Benchmark]
        public int IndexOfAny() =>
            "All the world's a stage, and all the men and women merely players: they have their exits and their entrances; and one man in his time plays many parts, his acts being seven ages."
            .IndexOfAny(s_colonAndSemicolon);

        [Benchmark]
        [Arguments("Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!", 8)]
        [Arguments("Testing {0}, {0:C}, {0:E} - {0:F4}{0:G}{0:N} , !!", 3.14159)]
        public string Format_OneArg(string s, object o)
            => string.Format(s, o);

        [Benchmark]
        public string Format_MultipleArgs()
            => string.Format("More testing: {0} {1} {2} {3} {4} {5}{6} {7}", '1', "Foo", "Foo", "Foo", "Foo", "Foo", "Foo", "Foo");

        [Benchmark]
        [Arguments('1', "Foo")]
        public string Interpolation_MultipleArgs(char c, string s)
            => $"More testing: {c} {s} {s} {s} {s} {s}{s} {s}"; 

        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        [Arguments("This is a much longer piece of text that might benefit more from vectorization.")]
        [MemoryRandomization]
        public string ToUpper(string s)
            => s.ToUpper();

        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        [Arguments("This is a much longer piece of text that might benefit more from vectorization.")]
        public string ToUpperInvariant(string s)
            => s.ToUpperInvariant();
        
        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        [Arguments("This is a much longer piece of text that might benefit more from vectorization.")]
        public string ToLower(string s)
            => s.ToLower();

        [Benchmark]
        [Arguments("TeSt")]
        [Arguments("TEST")]
        [Arguments("test")]
        [Arguments("This is a much longer piece of text that might benefit more from vectorization.")]
        [MemoryRandomization]
        public string ToLowerInvariant(string s)
            => s.ToLowerInvariant();

        private static readonly string _s1 =
            "ddsz dszdsz \t  dszdsz  a\u0300\u00C0 \t Te st \u0400Te \u0400st\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\t Te\t \tst \t\r\n\u0020\u00A0\u2000\u2001\u2002\u2003\u2004\u2005";

        [Benchmark]
        public int IndexerCheckBoundCheckHoist()
        {
            string s1 = _s1;
            int counter = 0;

            int strLength = _s1.Length;

            for (int j = 0; j < strLength; j++)
            {
                counter += s1[j];
            }

            return counter;
        }

        [Benchmark]
        public int IndexerCheckLengthHoisting()
        {
            string s1 = _s1;
            int counter = 0;

            for (int j = 0; j < s1.Length; j++)
            {
                counter += s1[j];
            }

            return counter;
        }

        [Benchmark]
        public int IndexerCheckPathLength()
        {
            string s1 = _s1;
            int counter = 0;

            for (int j = 0; j < s1.Length; j++)
            {
                counter += getStringCharNoInline(s1, j);
            }

            return counter;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static char getStringCharNoInline(string str, int index)
        {
            return str[index];
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

            TestString1 = PerfUtils.CreateString(size);
            TestString2 = PerfUtils.CreateString(size);
            TestString3 = PerfUtils.CreateString(size);
            TestString4 = PerfUtils.CreateString(size);

            Q1 = TestString1.Substring(0, TestString1.Length / 4);
            Q3 = TestString1.Substring(TestString1.Length / 2, TestString1.Length / 4);
        }
    }
}