// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    /// <summary>
    /// A <see cref="Stream"/> that reports that it read the number of bytes it was asked
    /// to read. Used for benchmarking wrappers around Stream without benchmarking the
    /// implementation of the inner Stream itself.
    /// </summary>
    internal sealed class DummyReadStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => count;

#if !NETFRAMEWORK // these virtual methods only exist in .NET Core 2.1+
        public override int Read(Span<byte> buffer) => buffer.Length;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.FromResult(buffer.Length);
#endif

        public override int ReadByte() => 0;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
