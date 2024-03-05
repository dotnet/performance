// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;
using System.Text;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class BinaryReaderTests
    {
        private BinaryReader _dummyReader;

        private MemoryStream _asciiChar, _nonAsciiChar;
        private BinaryReader _asciiCharReader, _nonAsciiCharReader;
        private MemoryStream _smallString, _largeString;
        private BinaryReader _smallStringReader, _largeStringReader;

        [GlobalSetup]
        public void Setup()
        {
            _dummyReader = new BinaryReader(new DummyReadStream());
            _asciiCharReader = new BinaryReader(_asciiChar = new MemoryStream(new byte[] { (byte)'a' }));
            _nonAsciiCharReader = new BinaryReader(_nonAsciiChar = new MemoryStream(new byte[] { 0xC3, 0xA0 /* U+00E0 */ }), Encoding.UTF8);
            _smallStringReader = new BinaryReader(_smallString = CreateMemoryStreamFromString("hello world"), Encoding.UTF8);

            _largeStringReader = new BinaryReader(_largeString = CreateMemoryStreamFromString(new string('a', 512)), Encoding.UTF8);

            static MemoryStream CreateMemoryStreamFromString(string value)
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream, Encoding.UTF8);
                writer.Write(value);
                return stream;
            }
        }

        [Benchmark]
        public BinaryReader DefaultCtor() => new BinaryReader(Stream.Null);

        [Benchmark]
        [MemoryRandomization]
        public bool ReadBool() => _dummyReader.ReadBoolean();

        [Benchmark]
        [MemoryRandomization]
        public char ReadAsciiChar()
        {
            _asciiChar.Position = 0;
            return _asciiCharReader.ReadChar();
        }

        [Benchmark]
        public char ReadNonAsciiChar()
        {
            _nonAsciiChar.Position = 0;
            return _nonAsciiCharReader.ReadChar();
        }

        [Benchmark]
        [MemoryRandomization]
        public ushort ReadUInt16() => _dummyReader.ReadUInt16();

        [Benchmark]
        public uint ReadUInt32() => _dummyReader.ReadUInt32();

        [Benchmark]
        public ulong ReadUInt64() => _dummyReader.ReadUInt64();

#if NET5_0_OR_GREATER
        [Benchmark]
        [MemoryRandomization]
        public Half ReadHalf() => _dummyReader.ReadHalf();
#endif

        [Benchmark]
        [MemoryRandomization]
        public float ReadSingle() => _dummyReader.ReadSingle();

        [Benchmark]
        public double ReadDouble() => _dummyReader.ReadDouble();

        [Benchmark]
        [MemoryRandomization]
        public string ReadSmallString()
        {
            _smallString.Position = 0;
            return _smallStringReader.ReadString();
        }

        [Benchmark]
        public string ReadLargeString()
        {
            _largeString.Position = 0;
            return _largeStringReader.ReadString();
        }
    }
}
