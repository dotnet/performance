// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Text.Tests
{
    public class Perf_Encoding
    {
        [Params(16, 512)]
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
        public byte[] GetBytes()
        {
            byte[] result = default;
            
            Encoding enc = _enc;
            string toEncode = _toEncode;

            for (int i = 0; i < 100; i++)
            {
                result = enc.GetBytes(toEncode); result = enc.GetBytes(toEncode); result = enc.GetBytes(toEncode);
                result = enc.GetBytes(toEncode); result = enc.GetBytes(toEncode); result = enc.GetBytes(toEncode);
                result = enc.GetBytes(toEncode); result = enc.GetBytes(toEncode); result = enc.GetBytes(toEncode);
            }

            return result;
        }

        [Benchmark]
        public string GetString()
        {
            string result = default;
            
            Encoding enc = _enc;
            byte[] bytes = _bytes;

            for (int i = 0; i < 100; i++)
            {
                result = enc.GetString(bytes); result = enc.GetString(bytes); result = enc.GetString(bytes);
                result = enc.GetString(bytes); result = enc.GetString(bytes); result = enc.GetString(bytes);
                result = enc.GetString(bytes); result = enc.GetString(bytes); result = enc.GetString(bytes);
            }

            return result;
        }

        [GlobalSetup(Target = nameof(GetString) + "," + nameof(GetChars))]
        public void SetupGetString()
        {
            _enc = Encoding.GetEncoding(encName);
            _bytes = _enc.GetBytes(_utils.CreateString(size));;
        }

        [Benchmark]
        public char[] GetChars()
        {
            char[] result = default;
            
            Encoding enc = _enc;
            byte[] bytes = _bytes;

            for (int i = 0; i < 100; i++)
            {
                result = enc.GetChars(bytes); result = enc.GetChars(bytes); result = enc.GetChars(bytes);
                result = enc.GetChars(bytes); result = enc.GetChars(bytes); result = enc.GetChars(bytes);
                result = enc.GetChars(bytes); result = enc.GetChars(bytes); result = enc.GetChars(bytes);
            }

            return result;
        }

        [GlobalSetup(Target = nameof(GetEncoder))]
        public void SetupGenEncoder() => _enc = Encoding.GetEncoding(encName);

        [Benchmark]
        public Encoder GetEncoder()
        {
            Encoder result = default;
            
            Encoding enc = _enc;

            for (int i = 0; i < 10000; i++)
            {
                result = enc.GetEncoder(); result = enc.GetEncoder(); result = enc.GetEncoder();
                result = enc.GetEncoder(); result = enc.GetEncoder(); result = enc.GetEncoder();
                result = enc.GetEncoder(); result = enc.GetEncoder(); result = enc.GetEncoder();
            }

            return result;
        }

        [GlobalSetup(Target = nameof(GetByteCount))]
        public void SetupGetByteCount()
        {
            _enc = Encoding.GetEncoding(encName);
            _chars = _utils.CreateString(size).ToCharArray();
        }

        [Benchmark]
        public int GetByteCount()
        {
            int result = default;
            
            Encoding enc = _enc;
            char[] chars = _chars;

            for (int i = 0; i < 100; i++)
            {
                result = enc.GetByteCount(chars); result = enc.GetByteCount(chars); result = enc.GetByteCount(chars);
                result = enc.GetByteCount(chars); result = enc.GetByteCount(chars); result = enc.GetByteCount(chars);
                result = enc.GetByteCount(chars); result = enc.GetByteCount(chars); result = enc.GetByteCount(chars);
            }

            return result;
        }
    }
}
