// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Span
{
    public class SpanBench
    {
        // Default length for arrays of mock input data
        const int DefaultLength = 1024;
        
        public IEnumerable<object> GetArrayOfBytes()
        {
            yield return new byte[DefaultLength];
        }
        
        public IEnumerable<object> GetArrayOfInts()
        {
            yield return new int[DefaultLength];
        }

        public IEnumerable<object> GetArrayOfStrings()
        {
            yield return new string[DefaultLength];
        }

        public IEnumerable<object> GetTestClassOfBytes()
        {
            yield return CreateTestClass<byte>();
        }
        
        public IEnumerable<object> GetTestClassOfString()
        {
            yield return CreateTestClass<string>();
        }

        private static TestClass<T> CreateTestClass<T>()
        {
            TestClass<T> testClass = new TestClass<T>();
            testClass.C0 = new T[DefaultLength];
            return testClass;
        }

        class Destination<T>
        {
            public T[] array = new T[DefaultLength];
            
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
        #endregion // helpers

        // Tests that implement some vary basic algorithms (fill/sort) over spans and arrays
        #region Algorithm tests

        #region TestFillAllSpan
        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void FillAllSpan(byte[] a) => TestFillAllSpan(a);

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
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void FillAllArray(byte[] a) => TestFillAllArray(a);

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
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void FillAllReverseSpan(byte[] a) => TestFillAllReverseSpan(a);

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
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void FillAllReverseArray(byte[] a) => TestFillAllReverseArray(a);

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
        public void SetupSort() => _unsortedData = GetUnsortedData(DefaultLength); 
        
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

        // TestSpanAPIs (For comparison with Array and Slow Span)
        #region TestSpanAPIs

        #region TestSpanConstructor<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public Span<byte> TestSpanConstructorByte(byte[] a) => new Span<byte>(a);

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public Span<string> TestSpanConstructorString(string[] a) => new Span<string>(a);
        
        #endregion

#if NETCOREAPP2_1 // netcoreapp specific API https://github.com/dotnet/coreclr/issues/16126
        #region TestSpanCreate<T>
        [Benchmark]
        [ArgumentsSource(nameof(GetTestClassOfBytes))]
        public Span<byte> TestSpanCreateByte(TestClass<byte> testClass) => MemoryMarshal.CreateSpan<byte>(ref testClass.C0[0], testClass.C0.Length);

        [Benchmark]
        [ArgumentsSource(nameof(GetTestClassOfString))]
        public Span<string> TestSpanCreateString(TestClass<string> testClass) => MemoryMarshal.CreateSpan<string>(ref testClass.C0[0], testClass.C0.Length);

        #endregion
#endif

        #region TestMemoryMarshalGetReference<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public ref byte TestMemoryMarshalGetReferenceByte(byte[] a) => ref MemoryMarshal.GetReference(new Span<byte>(a));

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public ref string TestMemoryMarshalGetReferenceString(string[] a) => ref MemoryMarshal.GetReference(new Span<string>(a));

        #endregion

        #region TestSpanIndexHoistable<T>
        // benchmarks removed => we have plenty of indexer benchmarks in Indexer.cs
        #endregion

        #region TestArrayIndexHoistable<T>
        // benchmarks removed => we have plenty of indexer benchmarks in Indexer.cs
        #endregion

        #region TestSpanIndexVariant<T>
        // benchmarks removed => we have plenty of indexer benchmarks in Indexer.cs
        #endregion

        #region TestArrayIndexVariant<T>
        // benchmarks removed => we have plenty of indexer benchmarks in Indexer.cs
        #endregion

        #region TestSpanSlice<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public Span<byte> TestSpanSliceByte(byte[] a) => new Span<byte>(a).Slice(a.Length / 2);

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public Span<string> TestSpanSliceString(string[]a) => new Span<string>(a).Slice(a.Length / 2);
        
        #endregion

        #region TestSpanToArray<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public byte[] TestSpanToArrayByte(byte[] a) => new Span<byte>(a).ToArray();

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public string[] TestSpanToArrayString(string[] a)=> new Span<string>(a).ToArray();
        
        #endregion

        #region TestSpanCopyTo<T>
        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void TestSpanCopyToByte(byte[] a) => new Span<byte>(a).CopyTo(Destination<byte>.Instance.array);

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public void TestSpanCopyToString(string[] a) => new Span<string>(a).CopyTo(Destination<string>.Instance.array);
        #endregion

        #region TestArrayCopyTo<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void TestArrayCopyToByte(byte[] a) => a.CopyTo(Destination<byte>.Instance.array, 0);

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public void TestArrayCopyToString(string[] a) => a.CopyTo(Destination<string>.Instance.array, 0);
        #endregion

        #region TestSpanFill<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void TestSpanFillByte(byte[] a) => new Span<byte>(a).Fill(default(byte));

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public void TestSpanFillString(string[] a) => new Span<string>(a).Fill(default(string));
        #endregion

        #region TestSpanClear<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void TestSpanClearByte(byte[] a) => TestSpanClear<byte>(a);

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public void TestSpanClearString(string[] a) => TestSpanClear<string>(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSpanClear<T>(Span<T> span) => span.Clear();

        #endregion

        #region TestArrayClear<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void TestArrayClearByte(byte[] a) => Array.Clear(a, 0, a.Length);

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfStrings))]
        public void TestArrayClearString(string[] a) => Array.Clear(a, 0, a.Length);

        #endregion

        #region TestSpanAsBytes<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public Span<byte> TestSpanAsBytesByte(byte[] a) => MemoryMarshal.AsBytes(new Span<byte>(a));

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfInts))]
        public Span<byte> TestSpanAsBytesInt(int[] a) => MemoryMarshal.AsBytes(new Span<int>(a));

        #endregion

        #region TestSpanCast<T>

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfBytes))]
        public void TestSpanCastFromByteToInt(byte[] a) => MemoryMarshal.Cast<byte, int>(a);

        [Benchmark]
        [ArgumentsSource(nameof(GetArrayOfInts))]
        public void TestSpanCastFromIntToByte(int[] a) => MemoryMarshal.Cast<int, byte>(a);

        #endregion

        #region TestSpanAsSpanStringChar<T>

        public IEnumerable<string> GetRandomString()
        {
            StringBuilder sb = new StringBuilder();
            Random rand = new Random(42);
            char[] c = new char[1];
            for (int i = 0; i < DefaultLength; i++)
            {
                c[0] = (char)rand.Next(32, 126);
                sb.Append(new string(c));
            }
            string s = sb.ToString();

            yield return s;
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetRandomString))]
        public ReadOnlySpan<char> TestSpanAsSpanStringCharWrapper(string s) => s.AsSpan();

        #endregion

        #endregion // TestSpanAPIs
    }
}
