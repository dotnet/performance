// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.IO.Compression
{
    public abstract class CompressionTestBase
    {
        public static IEnumerable<object> UncompressedTestFiles() => UncompressedTestFileNames();
        
        public static IEnumerable<string> UncompressedTestFileNames()
        {
            yield return "TestDocument.doc";
            yield return "TestDocument.docx";
            yield return "TestDocument.pdf";
            yield return "TestDocument.txt";
            yield return "alice29.txt";
            yield return "asyoulik.txt";
            yield return "cp.html";
            yield return "fields.c";
            yield return "grammar.lsp";
            yield return "kennedy.xls";
            yield return "lcet10.txt";
            yield return "plrabn12.txt";
            yield return "ptt5";
            yield return "sum";
            yield return "xargs.1";
        }

        protected string GetFilePath(string fileName) => Path.Combine(@"corefx\System.IO.Compression\TestData", fileName);
    }

    public abstract class CompressionStreamTestBase : CompressionTestBase
    {
        public abstract Stream CreateStream(Stream stream, CompressionMode mode);
        public abstract Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen);
        public abstract Stream BaseStream(Stream stream);
        public virtual bool FlushCompletes { get => true; }
        public virtual bool FlushNoOps { get => false; }
        public virtual int BufferSize { get => 8192; }
    }
}
