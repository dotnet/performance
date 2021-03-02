// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    /// <summary>
    /// A <see cref="Stream"/> that acts as a null sink for data. Overrides members that
    /// <see cref="Stream.Null"/> does not. Used for benchmarking wrappers around Stream
    /// without benchmarking the implementation of the inner Stream itself.
    /// </summary>
    internal sealed class NullWriteStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) { }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.CompletedTask;

        public override void WriteByte(byte value) { }

#if NETCOREAPP2_1_OR_GREATER // these virtual methods only exist in .NET Core 2.1+
        public override void Write(ReadOnlySpan<byte> buffer) { }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => new ValueTask(Task.CompletedTask);
#endif
    }
}
