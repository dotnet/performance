using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Linq;

namespace System.Text
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Ascii
    {
        [Params(
            6, // non-vectorized code path
            128)] // vectorized code path
        public int Size;

        private byte[] _bytes, _sameBytes, _bytesDifferentCase;
        private char[] _characters, _sameCharacters, _charactersDifferentCase;

        [GlobalSetup]
        public void Setup()
        {
            _bytes = new byte[Size];
            _bytesDifferentCase = new byte[Size];

            for (int i = 0; i < Size; i++)
            {
                // let ToLower and ToUpper perform the same amount of work
                _bytes[i] = i % 2 == 0 ? (byte)'a' : (byte)'A';
                _bytesDifferentCase[i] = i % 2 == 0 ? (byte)'A' : (byte)'a';
            }
            _sameBytes = _bytes.ToArray();
            _characters = _bytes.Select(b => (char)b).ToArray();
            _sameCharacters = _characters.ToArray();
            _charactersDifferentCase = _bytesDifferentCase.Select(b => (char)b).ToArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public bool IsValid_Bytes() => Ascii.IsValid(_bytes);

        [Benchmark]
        public bool IsValid_Chars() => Ascii.IsValid(_characters);

        [Benchmark]
        public bool Equals_Bytes() => Ascii.Equals(_bytes, _sameBytes);

        [Benchmark]
        [MemoryRandomization]
        public bool Equals_Chars() => Ascii.Equals(_characters, _sameCharacters);

        [Benchmark]
        [MemoryRandomization]
        public bool Equals_Bytes_Chars() => Ascii.Equals(_bytes, _characters);

        [Benchmark]
        public bool EqualsIgnoreCase_ExactlyTheSame_Bytes() => Ascii.EqualsIgnoreCase(_bytes, _sameBytes);

        [Benchmark]
        [MemoryRandomization]
        public bool EqualsIgnoreCase_ExactlyTheSame_Chars() => Ascii.EqualsIgnoreCase(_characters, _sameCharacters);

        [Benchmark]
        [MemoryRandomization]
        public bool EqualsIgnoreCase_ExactlyTheSame_Bytes_Chars() => Ascii.EqualsIgnoreCase(_bytes, _characters);

        [Benchmark]
        [MemoryRandomization]
        public bool EqualsIgnoreCase_DifferentCase_Bytes() => Ascii.EqualsIgnoreCase(_bytes, _bytesDifferentCase);

        [Benchmark]
        [MemoryRandomization]
        public bool EqualsIgnoreCase_DifferentCase_Chars() => Ascii.EqualsIgnoreCase(_characters, _charactersDifferentCase);

        [Benchmark]
        [MemoryRandomization]
        public bool EqualsIgnoreCase_DifferentCase_Bytes_Chars() => Ascii.EqualsIgnoreCase(_bytes, _charactersDifferentCase);

        [Benchmark]
        public OperationStatus ToLower_Bytes() => Ascii.ToLower(_bytes, _sameBytes, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus ToLower_Chars() => Ascii.ToLower(_characters, _sameCharacters, out _);

        [Benchmark]
        public OperationStatus ToLower_Bytes_Chars() => Ascii.ToLower(_bytes, _characters, out _);

        [Benchmark]
        public OperationStatus ToUpper_Bytes() => Ascii.ToUpper(_bytes, _sameBytes, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus ToUpper_Chars() => Ascii.ToUpper(_characters, _sameCharacters, out _);

        [Benchmark]
        public OperationStatus ToUpper_Bytes_Chars() => Ascii.ToUpper(_bytes, _characters, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus ToLowerInPlace_Bytes() => Ascii.ToLowerInPlace(_bytes, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus ToLowerInPlace_Chars() => Ascii.ToLowerInPlace(_characters, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus ToUpperInPlace_Bytes() => Ascii.ToUpperInPlace(_bytes, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus ToUpperInPlace_Chars() => Ascii.ToUpperInPlace(_characters, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus ToUtf16() => Ascii.ToUtf16(_bytes, _characters, out _);

        [Benchmark]
        [MemoryRandomization]
        public OperationStatus FromUtf16() => Ascii.FromUtf16(_characters, _bytes, out _);
    }
}
