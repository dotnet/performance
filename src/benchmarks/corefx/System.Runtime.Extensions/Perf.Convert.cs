// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Convert
    {
        private static byte[] InitializeBinaryDataCollection(int size)
        {
            var random = new Random(30000);
            byte[] binaryData = new byte[size];
            random.NextBytes(binaryData);

            return binaryData;
        }

        private const int Size = 1024;
        
        private object _stringValue = "Hello World!";
        private object _intValue = 1000;
        private byte[] _binaryData = InitializeBinaryDataCollection(Size);
        private char[] _base64CharArray;

        [Benchmark]
        public TypeCode GetTypeCode() => Convert.GetTypeCode(_stringValue);

        [Benchmark]
        public object ChangeType() => Convert.ChangeType(_intValue, typeof(string));

        [GlobalSetup(Target = nameof(ToBase64CharArray))]
        public void SetupToBase64CharArray()
        {
            int insertLineBreaksArraySize = Convert.ToBase64String(_binaryData, Base64FormattingOptions.InsertLineBreaks).Length;
            int noneArraySize = Convert.ToBase64String(_binaryData, Base64FormattingOptions.None).Length;
            _base64CharArray = new char[Math.Max(noneArraySize, insertLineBreaksArraySize)];
        }
        
        [Benchmark]
        [Arguments(Size, Base64FormattingOptions.InsertLineBreaks)]
        [Arguments(Size, Base64FormattingOptions.None)]
        public int ToBase64CharArray(int binaryDataSize, Base64FormattingOptions formattingOptions)
            => Convert.ToBase64CharArray(_binaryData, 0, binaryDataSize, _base64CharArray, 0, formattingOptions);

        [Benchmark]
        [Arguments(Base64FormattingOptions.InsertLineBreaks)]
        [Arguments(Base64FormattingOptions.None)]
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
    }
}
