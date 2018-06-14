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

        private TestClass<byte> testClassByte; 
        private TestClass<string> testClassString; 

        [GlobalSetup]
        public void Setup()
        {
            bytes = new byte[length];
            ints = new int[length];
            strings = new string[length];
            randomString = GetRandomString();
            testClassByte = CreateTestClass<byte>();
            testClassString = CreateTestClass<string>();
        }

        private TestClass<T> CreateTestClass<T>()
        {
            TestClass<T> testClass = new TestClass<T>();
            testClass.C0 = new T[length];
            return testClass;
        }

        class Destination<T>
        {
            public T[] array = new T[SpanBench.length]; // always more than length
            
            public static Destination<T> Instance = new Destination<T>();
        }

        // Helpers
        #region Helpers
        
        [StructLayout(LayoutKind.Sequential)]
        public sealed class TestClass<T>
        {
            private double _d;
            public T[] C0;
        }

        // Helper for the sort tests to get some pseudo-random input
        static int[] GetUnsortedData(int length)
        {
            int[] unsortedData = new int[length];
            Random r = new Random(42);
            for (int i = 0; i < unsortedData.Length; ++i)
            {
                unsortedData[i] = r.Next();
            }
            return unsortedData;
        }
        
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

        // Tests that implement some vary basic algorithms (fill/sort) over spans and arrays
        #region Algorithm tests

        #region TestFillAllSpan
        [Benchmark]
        public void FillAllSpan() => TestFillAllSpan(bytes);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllSpan(Span<byte> span)
        {
            for (int i = 0; i < span.Length; ++i)
            {
                span[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestFillAllArray
        
        [Benchmark]
        public void FillAllArray() => TestFillAllArray(bytes);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllArray(byte[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestFillAllReverseSpan

        [Benchmark]
        public void FillAllReverseSpan() => TestFillAllReverseSpan(bytes);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllReverseSpan(Span<byte> span)
        {
            for (int i = span.Length; --i >= 0;)
            {
                span[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestFillAllReverseArray
        [Benchmark]
        public void FillAllReverseArray() => TestFillAllReverseArray(bytes);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestFillAllReverseArray(byte[] data)
        {
            for (int i = data.Length; --i >= 0;)
            {
                data[i] = unchecked((byte)i);
            }
        }
        #endregion

        #region TestQuickSortSpan
        
        int[] _unsortedData;
        
        [IterationSetup(Target = nameof(QuickSortSpan) + "," + nameof(BubbleSortSpan) + "," + nameof(QuickSortArray) + "," + nameof(BubbleSortArray))]
        public void SetupSort() => _unsortedData = GetUnsortedData(length); 
        
        [Benchmark]
        public void QuickSortSpan() => TestQuickSortSpan(_unsortedData);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestQuickSortSpan(Span<int> data)
        {
            if (data.Length <= 1)
            {
                return;
            }

            int lo = 0;
            int hi = data.Length - 1;
            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;)
            {
                while (i < j && data[i] <= pivot)
                {
                    ++i;
                }
                while (j > i && data[j] >= pivot)
                {
                    --j;
                }
                if (i < j)
                {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi)
            {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            TestQuickSortSpan(data.Slice(0, i));
            TestQuickSortSpan(data.Slice(i + 1));
        }
        #endregion

        #region TestBubbleSortSpan
        [Benchmark]
        public void BubbleSortSpan() => TestBubbleSortSpan(_unsortedData);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortSpan(Span<int> span)
        {
            bool swap;
            int temp;
            int n = span.Length - 1;
            do
            {
                swap = false;
                for (int i = 0; i < n; i++)
                {
                    if (span[i] > span[i + 1])
                    {
                        temp = span[i];
                        span[i] = span[i + 1];
                        span[i + 1] = temp;
                        swap = true;
                    }
                }
                --n;
            }
            while (swap);
        }
        #endregion

        #region TestQuickSortArray
        [Benchmark]
        public void QuickSortArray() => TestQuickSortArray(_unsortedData, 0, _unsortedData.Length - 1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestQuickSortArray(int[] data, int lo, int hi)
        {
            if (lo >= hi)
            {
                return;
            }

            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;)
            {
                while (i < j && data[i] <= pivot)
                {
                    ++i;
                }
                while (j > i && data[j] >= pivot)
                {
                    --j;
                }
                if (i < j)
                {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi)
            {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            TestQuickSortArray(data, lo, i - 1);
            TestQuickSortArray(data, i + 1, hi);
        }
        #endregion

        #region TestBubbleSortArray
        [Benchmark]
        public void BubbleSortArray() => TestBubbleSortArray(_unsortedData);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortArray(int[] data)
        {
            bool swap;
            int temp;
            int n = data.Length - 1;
            do
            {
                swap = false;
                for (int i = 0; i < n; i++)
                {
                    if (data[i] > data[i + 1])
                    {
                        temp = data[i];
                        data[i] = data[i + 1];
                        data[i + 1] = temp;
                        swap = true;
                    }
                }
                --n;
            }
            while (swap);
        }
        #endregion

        #endregion // Algorithm tests

        [Benchmark]
        public Span<byte> TestSpanConstructorByte() => new Span<byte>(bytes);

        [Benchmark]
        public Span<string> TestSpanConstructorString() => new Span<string>(strings);

#if NETCOREAPP2_1 // netcoreapp specific API https://github.com/dotnet/coreclr/issues/16126
        [Benchmark]
        public Span<byte> TestSpanCreateByte() => MemoryMarshal.CreateSpan<byte>(ref testClassByte.C0[0], testClassByte.C0.Length);

        [Benchmark]
        public Span<string> TestSpanCreateString() => MemoryMarshal.CreateSpan<string>(ref testClassString.C0[0], testClassString.C0.Length);
#endif

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
        public void TestSpanCopyToByte() => new Span<byte>(bytes).CopyTo(Destination<byte>.Instance.array);

        [Benchmark]
        public void TestSpanCopyToString() => new Span<string>(strings).CopyTo(Destination<string>.Instance.array);

        [Benchmark]
        public void TestArrayCopyToByte() => bytes.CopyTo(Destination<byte>.Instance.array, 0);

        [Benchmark]
        public void TestArrayCopyToString() => strings.CopyTo(Destination<string>.Instance.array, 0);

        [Benchmark]
        public void TestSpanFillByte() => new Span<byte>(bytes).Fill(default(byte));

        [Benchmark]
        public void TestSpanFillString() => new Span<string>(strings).Fill(default(string));

        [Benchmark]
        public void TestSpanClearByte() => TestSpanClear<byte>(bytes);

        [Benchmark]
        public void TestSpanClearString() => TestSpanClear<string>(strings);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanClear<T>(Span<T> span) => span.Clear();

        [Benchmark]
        public void TestArrayClearByte() => Array.Clear(bytes, 0, bytes.Length);

        [Benchmark]
        public void TestArrayClearString() => Array.Clear(strings, 0, strings.Length);

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
