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
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "TestDocument.doc");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "TestDocument.docx");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "TestDocument.pdf");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "TestDocument.txt");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "alice29.txt");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "asyoulik.txt");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "cp.html");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "fields.c");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "grammar.lsp");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "kennedy.xls");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "lcet10.txt");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "plrabn12.txt");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "ptt5");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "sum");
            yield return Path.Combine(@"corefx\System.IO.Compression\TestData", "xargs.1");
        }
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
