// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.IO.Compression
{
    public class Gzip : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new GZipStream(stream, level);
    }
    
    public class Deflate : CompressionStreamPerfTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new DeflateStream(stream, level);
    }
    
    // Brotli has a dedicated file with more benchmarks
    
    public abstract class CompressionStreamPerfTestBase
    {
        public abstract Stream CreateStream(Stream stream, CompressionMode mode);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level);

        public IEnumerable<object> Arguments()
        {
            foreach (string testFile in UncompressedTestFileNames())
            {
                yield return new CompressedFile(testFile, CompressionLevel.Optimal, CreateStream);
                yield return new CompressedFile(testFile, CompressionLevel.Fastest, CreateStream);
                // we don't test the performance of CompressionLevel.NoCompression on purpose
            }
        }

        private IEnumerable<string> UncompressedTestFileNames()
        {
            // yield return "TestDocument.doc"; // 44.5 KB small test document with repeated paragraph
            // yield return "TestDocument.docx"; // 17.2 KB small test document with repeated paragraph
            yield return "TestDocument.pdf"; // 199 KB small test document with repeated paragraph, PDF are common
            // yield return "TestDocument.txt"; // 21.1 KB small test document with repeated paragraph
            yield return "alice29.txt"; // 145 KB, copy of "ALICE'S ADVENTURES IN WONDERLAND" book, an example of text file
            // yield return "asyoulik.txt"; // 122 KB, copy if "As You Like It" by William Shakespeare
            // yield return "cp.html"; // 24 KB, small HTML file
            // yield return "fields.c"; // 10.8 KB, 430 lines of C code
            // yield return "grammar.lsp"; // 3.63 KB, 90 lines of Lisp code
            // yield return "kennedy.xls"; // 0.98 MB, invalid excel file..
            // yield return "lcet10.txt"; // 409 KB, "The Project Gutenberg Etext of LOC WORKSHOP ON ELECTRONIC TEXTS", 7500 lines of text
            // yield return "plrabn12.txt"; // 460 KB, "Paradise Lost by John Milton", 10700 lines of text
            // yield return "ptt5"; // 501 KB, some binary content
            yield return "sum"; // 37.3 KB, some binary content, an example of binary file
            // yield return "xargs.1"; // 4.12 KB, output of --help of some Linux tool
        }

        [Benchmark]
        [ArgumentsSource(nameof(Arguments))]
        public void Compress(CompressedFile file)
        {
            file.CompressedDataStream.Position = 0; // all benchmarks invocation reuse the same stream, we set Postion to 0 to start at the beginning

            var compressor = CreateStream(file.CompressedDataStream, file.CompressionLevel);
            compressor.Write(file.UncompressedData, 0, file.UncompressedData.Length);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Arguments))]
        public int Decompress(CompressedFile file)
        {
            file.CompressedDataStream.Position = 0;

            var compressor = CreateStream(file.CompressedDataStream, CompressionMode.Decompress);
            return compressor.Read(file.UncompressedData, 0, file.UncompressedData.Length);
        }
    }
}
