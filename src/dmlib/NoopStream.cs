using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace AzDataMovementTest
{
    internal class NoopStream : Stream
    {
        public override bool CanRead {get;} = false;
        public override bool CanSeek {get;} = true;
        public override bool CanTimeout {get;} = false;
        public override bool CanWrite { get;} = true;
        public override long Position { get; set; } = 0;

        private long length = 0;

        public NoopStream() { }

        public override long Seek(long pos, SeekOrigin origin) { return 0; }

        public override void Write(byte[] buffer, int offset, int count) { length += count; }

        public override void SetLength(long len) { }
        public override long Length { get { return length; } }
        public override int Read(byte[] buffer, int offset, int len) { throw new NotSupportedException(); }
        public override void Flush() { }
    }
}
