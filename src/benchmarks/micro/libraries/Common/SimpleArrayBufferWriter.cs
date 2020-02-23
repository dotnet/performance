using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MicroBenchmarks.libraries.Common
{
    public sealed class SimpleArrayBufferWriter<T> : IBufferWriter<T>
    {
        private readonly T[] _array;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SimpleArrayBufferWriter(int length)
        {
            _array = new T[length];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            var index = _index + count;
            if (index >= _array.Length)
                ThrowInvalidOperationException();
            _index = index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> GetMemory(int sizeHint = 0)
        {
            if (sizeHint == 0)
                sizeHint = 1; // at least 1
            if (_index + sizeHint >= _array.Length)
                ThrowOutOfMemoryException();
            return _array.AsMemory(_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetSpan(int sizeHint = 0)
        {
            if (sizeHint == 0)
                sizeHint = 1; // at least 1
            if (_index + sizeHint >= _array.Length)
                ThrowOutOfMemoryException();
            return _array.AsSpan(_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _index = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutOfMemoryException()
        {
            throw new OutOfMemoryException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationException()
        {
            throw new InvalidOperationException();
        }
    }
}
