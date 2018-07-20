// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace Span
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Span)]
    public class SpanBench
    {
        [Params(1024)]
        public static int length; // the field must be called length (starts with lowercase) to keep old benchmark id in BenchView, do NOT change it

        private byte[] bytes;
        private int[] ints;
        private string[] strings;
        private string randomString;


        [GlobalSetup]
        public void Setup()
        {
            bytes = new byte[length];
            ints = new int[length];
            strings = new string[length];
            randomString = GetRandomString();
        }
        // Helpers
        #region Helpers
        
        private string GetRandomString()
        {
            StringBuilder sb = new StringBuilder();
            Random rand = new Random(42);
            char[] c = new char[1];
            for (int i = 0; i < length; i++)
            {
                c[0] = (char)rand.Next(32, 126);
                sb.Append(new string(c));
            }
            string s = sb.ToString();

            return s;
        }
        #endregion // helpers

        [Benchmark]
        public ref byte TestMemoryMarshalGetReferenceByte() => ref MemoryMarshal.GetReference(new Span<byte>(bytes));

        [Benchmark]
        public ref string TestMemoryMarshalGetReferenceString() => ref MemoryMarshal.GetReference(new Span<string>(strings));

        [Benchmark]
        public Span<byte> TestSpanSliceByte() => new Span<byte>(bytes).Slice(bytes.Length / 2);

        [Benchmark]
        public Span<string> TestSpanSliceString() => new Span<string>(strings).Slice(strings.Length / 2);

        [Benchmark]
        public byte[] TestSpanToArrayByte() => new Span<byte>(bytes).ToArray();

        [Benchmark]
        public string[] TestSpanToArrayString()=> new Span<string>(strings).ToArray();

        [Benchmark]
        public void TestSpanFillByte() => new Span<byte>(bytes).Fill(default(byte));

        [Benchmark]
        public void TestSpanFillString() => new Span<string>(strings).Fill(default(string));

        [Benchmark]
        public Span<byte> TestSpanAsBytesByte() => MemoryMarshal.AsBytes(new Span<byte>(bytes));

        [Benchmark]
        public Span<byte> TestSpanAsBytesInt() => MemoryMarshal.AsBytes(new Span<int>(ints));

        [Benchmark]
        public void TestSpanCastFromByteToInt() => MemoryMarshal.Cast<byte, int>(bytes);

        [Benchmark]
        public void TestSpanCastFromIntToByte() => MemoryMarshal.Cast<int, byte>(ints);

        [Benchmark]
        public ReadOnlySpan<char> TestSpanAsSpanStringCharWrapper() => randomString.AsSpan();
    }
}
