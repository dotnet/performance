// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json.Tests
{
    // Remove once we have https://github.com/dotnet/corefx/issues/34894
    internal sealed class ArrayBufferWriter<T> : IBufferWriter<T>
    {
        private T[] _buffer;
        private int _index;

        private const int MinimumBufferSize = 256;

        public ArrayBufferWriter()
        {
            _buffer = new T[MinimumBufferSize];
            _index = 0;
        }

        public ArrayBufferWriter(int initialCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException(nameof(initialCapacity));

            _buffer = new T[initialCapacity];
            _index = 0;
        }

        public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

        public int WrittenCount => _index;

        public int Capacity => _buffer.Length;

        public int FreeCapacity => _buffer.Length - _index;

        public void Clear()
        {
            _buffer.AsSpan(0, _index).Clear();
            _index = 0;
        }

        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentException(nameof(count));

            if (_index > _buffer.Length - count)
                throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {_buffer.Length}.");

            _index += count;
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            if (sizeHint == 0)
            {
                sizeHint = MinimumBufferSize;
            }

            int availableSpace = _buffer.Length - _index;

            if (sizeHint > availableSpace)
            {
                int growBy = Math.Max(sizeHint, _buffer.Length);

                int newSize = checked(_buffer.Length + growBy);

                var newBuffer = new T[newSize];

                Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _index);

                _buffer = newBuffer;
            }
        }
    }
}
