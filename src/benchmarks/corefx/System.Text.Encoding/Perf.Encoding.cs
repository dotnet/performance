// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Text.Tests
{
    public class Perf_Encoding
    {
        [Params(16, 32, 64, 128, 256, 512, 10000, 1000000)]
        public int size; // the field must be called length (starts with lowercase) to keep old benchmark id in BenchView, do NOT change it
        
        [Params("utf-8", "ascii")]
        public string encName; // the field must be called length (starts with lowercase) to keep old benchmark id in BenchView, do NOT change it

        private readonly PerfUtils _utils = new PerfUtils();
        
        private Encoding _enc;
        private string _toEncode;
        private byte[] _bytes;
        private char[] _chars;

        [GlobalSetup(Target = nameof(GetBytes))]
        public void SetupGetBytes()
        {
            _enc = Encoding.GetEncoding(encName);
            _toEncode = _utils.CreateString(size);
        }

        [Benchmark]
        public void GetBytes()
        {
            const int innerIterations = 100;
            
            Encoding enc = _enc;
            string toEncode = _toEncode;

            for (int i = 0; i < innerIterations; i++)
            {
                enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
            }
        }
        
        [GlobalSetup(Target = nameof(GetString) + "," + nameof(GetChars))]
        public void SetupGetString()
        {
            _enc = Encoding.GetEncoding(encName);
            _bytes = _enc.GetBytes(_utils.CreateString(size));;
        }

        [Benchmark]
        public void GetString()
        {
            const int innerIterations = 100;
            
            Encoding enc = _enc;
            byte[] bytes = _bytes;

            for (int i = 0; i < innerIterations; i++)
            {
                enc.GetString(bytes); enc.GetString(bytes); enc.GetString(bytes);
                enc.GetString(bytes); enc.GetString(bytes); enc.GetString(bytes);
                enc.GetString(bytes); enc.GetString(bytes); enc.GetString(bytes);
            }
        }

        [Benchmark]
        public void GetChars()
        {
            const int innerIterations = 100;
            
            Encoding enc = _enc;
            byte[] bytes = _bytes;

            for (int i = 0; i < innerIterations; i++)
            {
                enc.GetChars(bytes); enc.GetChars(bytes); enc.GetChars(bytes);
                enc.GetChars(bytes); enc.GetChars(bytes); enc.GetChars(bytes);
                enc.GetChars(bytes); enc.GetChars(bytes); enc.GetChars(bytes);
            }
        }

        [GlobalSetup(Target = nameof(GetEncoder))]
        public void SetupGenEncoder() => _enc = Encoding.GetEncoding(encName);

        [Benchmark]
        public void GetEncoder()
        {
            const int innerIterations = 10000;
            var enc = _enc;
            
            for (int i = 0; i < innerIterations; i++)
            {
                enc.GetEncoder(); enc.GetEncoder(); enc.GetEncoder();
                enc.GetEncoder(); enc.GetEncoder(); enc.GetEncoder();
                enc.GetEncoder(); enc.GetEncoder(); enc.GetEncoder();
            }
        }

        [GlobalSetup(Target = nameof(GetByteCount))]
        public void SetupGetByteCount()
        {
            _enc = Encoding.GetEncoding(encName);
            _chars = _utils.CreateString(size).ToCharArray();
        }

        [Benchmark]
        public void GetByteCount()
        {
            const int innerIterations = 100;
            
            Encoding enc = _enc;
            char[] chars = _chars;
            
            for (int i = 0; i < innerIterations; i++)
            {
                enc.GetByteCount(chars); enc.GetByteCount(chars); enc.GetByteCount(chars);
                enc.GetByteCount(chars); enc.GetByteCount(chars); enc.GetByteCount(chars);
                enc.GetByteCount(chars); enc.GetByteCount(chars); enc.GetByteCount(chars);
            }
        }
    }
}
