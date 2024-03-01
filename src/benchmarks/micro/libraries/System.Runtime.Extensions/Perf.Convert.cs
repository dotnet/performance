// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Text;

namespace System
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Convert
    {
        private const int Size = 1024;
        
        private object _stringValue;
        private object _intValue;
        private byte[] _binaryData;
        private char[] _base64CharArray;
        private string _base64String;
#if NET5_0_OR_GREATER
        private string _hexString;
#endif
        private char[] _base64Chars;

        [GlobalSetup(Target = nameof(GetTypeCode))]
        public void SetupGetTypeCode() => _stringValue = "Hello World!";

        [Benchmark]
        [MemoryRandomization]
        public TypeCode GetTypeCode() => Convert.GetTypeCode(_stringValue);

        [GlobalSetup(Target = nameof(ChangeType))]
        public void SetupChangeType() => _intValue = 1000;

        [Benchmark]
        public object ChangeType() => Convert.ChangeType(_intValue, typeof(string));

        [GlobalSetup(Target = nameof(ToBase64CharArray))]
        public void SetupToBase64CharArray()
        {
            _binaryData = InitializeBinaryDataCollection(Size);
            int insertLineBreaksArraySize = Convert.ToBase64String(_binaryData, Base64FormattingOptions.InsertLineBreaks).Length;
            int noneArraySize = Convert.ToBase64String(_binaryData, Base64FormattingOptions.None).Length;
            _base64CharArray = new char[Math.Max(noneArraySize, insertLineBreaksArraySize)];
        }
        
        [Benchmark]
        [Arguments(Size, Base64FormattingOptions.InsertLineBreaks)]
        [Arguments(Size, Base64FormattingOptions.None)]
        [MemoryRandomization]
        public int ToBase64CharArray(int binaryDataSize, Base64FormattingOptions formattingOptions)
            => Convert.ToBase64CharArray(_binaryData, 0, binaryDataSize, _base64CharArray, 0, formattingOptions);

        [GlobalSetup(Target = nameof(ToBase64String))]
        public void SetupToBase64String() => _binaryData = InitializeBinaryDataCollection(Size);

        [Benchmark]
        [Arguments(Base64FormattingOptions.InsertLineBreaks)]
        [Arguments(Base64FormattingOptions.None)]
        [MemoryRandomization]
        public string ToBase64String(Base64FormattingOptions formattingOptions)
            => Convert.ToBase64String(_binaryData, formattingOptions);

        [Benchmark]
        [Arguments("Fri, 27 Feb 2009 03:11:21 GMT")]
        [Arguments("Thursday, February 26, 2009")]
        [Arguments("February 26, 2009")]
        [Arguments("12/12/1999 11:59:59 PM")]
        [Arguments("12/12/1999")]
        public DateTime ToDateTime_String(string value) 
            => Convert.ToDateTime(value);

        [GlobalSetup(Target = nameof(FromBase64String))]
        public void SetupFromBase64String() => _base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes("This is a test of Convert."));

        [Benchmark]
        public byte[] FromBase64String() => Convert.FromBase64String(_base64String);

        [GlobalSetup(Target = nameof(FromBase64Chars))]
        public void SetupFromBase64Chars() => _base64Chars = Convert.ToBase64String(Encoding.ASCII.GetBytes("This is a test of Convert.")).ToCharArray();

        [Benchmark]
        public byte[] FromBase64Chars() => Convert.FromBase64CharArray(_base64Chars, 0, _base64Chars.Length);

#if NET5_0_OR_GREATER
        [GlobalSetup(Target = nameof(ToHexString))]
        public void SetupToHexString()
        {
            _binaryData = new byte[64];
            new Random(42).NextBytes(_binaryData);
        }

        [GlobalSetup(Target = nameof(FromHexString))]
        public void SetupFromHexString()
        {
            _hexString = Convert.ToHexString(Encoding.ASCII.GetBytes("This is a test of Convert."));   
        }

        [Benchmark]
        public string ToHexString() => Convert.ToHexString(_binaryData);
        [Benchmark]
        public byte[] FromHexString() => Convert.FromHexString(_hexString);
#endif

        private static byte[] InitializeBinaryDataCollection(int size)
        {
            var random = new Random(30000);
            byte[] binaryData = new byte[size];
            random.NextBytes(binaryData);

            return binaryData;
        }
    }
}
