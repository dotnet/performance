// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_StreamWriter
    {
        private const int MemoryStreamSize = 32768;
        private const int TotalWriteCount = 16777216; // 2^24 - should yield around 300ms runs
        private const int DefaultStreamWriterBufferSize = 1024; // Same as StreamWriter internal default

        private string _string2, _string100;
        private char[] _buffer2, _buffer100;
        private char[] _buffer12, _buffer110;
        private MemoryStream _memoryStream;
        private StreamWriter _streamWriter;

        public IEnumerable<object> WriteLengthMemberData()
        {
            yield return 2;
            yield return 100;
        }

        [GlobalSetup]
        public void Setup()
        {
            _string2 = new string('a', 2); _string100 = new string('a', 100);
            _buffer2 = new string('a', 2).ToCharArray(); _buffer100 = new string('a', 100).ToCharArray();
            _buffer12 = new string('a', 12).ToCharArray(); _buffer110 = new string('a', 110).ToCharArray();

            _memoryStream = new MemoryStream(MemoryStreamSize);
            _streamWriter = new StreamWriter(_memoryStream, new UTF8Encoding(false, true), DefaultStreamWriterBufferSize, leaveOpen: true);
        }
        
        [GlobalCleanup]
        public void Cleanup()
        {
            _streamWriter.Dispose();
            _memoryStream.Dispose();
        }

        [Benchmark]
        [ArgumentsSource(nameof(WriteLengthMemberData))]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void WriteCharArray(int writeLength)
        {
            char[] buffer = writeLength == 2 ? _buffer2 : _buffer100;
            int innerIterations = MemoryStreamSize / writeLength;
            int outerIteration = TotalWriteCount / innerIterations;

            var writer = _streamWriter;
            var stream = _memoryStream;
            
            for (int i = 0; i < outerIteration; i++)
            {
                for (int j = 0; j < innerIterations; j++)
                {
                    writer.Write(buffer);
                }
                writer.Flush();
                stream.Position = 0;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(WriteLengthMemberData))]
        [BenchmarkCategory(Categories.NoInterpreter)]
        [MemoryRandomization]
        public void WritePartialCharArray(int writeLength)
        {
            char[] buffer = writeLength == 2 ? _buffer12 : _buffer110;
            int innerIterations = MemoryStreamSize / writeLength;
            int outerIteration = TotalWriteCount / innerIterations;

            var writer = _streamWriter;
            var stream = _memoryStream;
            
            for (int i = 0; i < outerIteration; i++)
            {
                for (int j = 0; j < innerIterations; j++)
                {
                    writer.Write(buffer, 10, writeLength);
                }
                writer.Flush();
                stream.Position = 0;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(WriteLengthMemberData))]
        [BenchmarkCategory(Categories.NoInterpreter)]
        public void WriteString(int writeLength)
        {
            string value = writeLength == 2 ? _string2 : _string100;
            int innerIterations = MemoryStreamSize / writeLength;
            int outerIteration = TotalWriteCount / innerIterations;

            var writer = _streamWriter;
            var stream = _memoryStream;
            
            for (int i = 0; i < outerIteration; i++)
            {
                for (int j = 0; j < innerIterations; j++)
                {
                    writer.Write(value);
                }
                writer.Flush();
                stream.Position = 0;
            }
        }

        [Benchmark]
        public void WriteFormat()
        {
            _memoryStream.Position = 0;
            _streamWriter.Write("Writing out a value: {0}", 42);
        }
    }
}
