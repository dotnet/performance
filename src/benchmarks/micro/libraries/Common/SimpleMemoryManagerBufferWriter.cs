using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace MicroBenchmarks.libraries.Common
{
    public sealed class SimpleMemoryManagerBufferWriter<T> : MemoryManager<T>, IBufferWriter<T>
    {
        private int refCount = 0;
        private IntPtr memory;
        private int length;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SimpleMemoryManagerBufferWriter(int length)
        {
            this.memory = Marshal.AllocHGlobal(Marshal.SizeOf<T>() * length);
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            var index = _index + count;
            if (index >= length)
                ThrowInvalidOperationException();
            _index = index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> GetMemory(int sizeHint = 0)
        {
            if (sizeHint == 0)
                sizeHint = 1; // at least 1
            if (_index + sizeHint > length)
                ThrowOutOfMemoryException();
            return this.Memory.Slice(_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<T> GetSpan(int sizeHint = 0)
        {
            if (sizeHint == 0)
                sizeHint = 1; // at least 1
            int spanLength = length - _index;
            if (spanLength < sizeHint)
                ThrowOutOfMemoryException();
            return new Span<T>(Unsafe.Add<T>((void*)memory, _index), spanLength);
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

        public bool IsDisposed { get; private set; }

        ~SimpleMemoryManagerBufferWriter()
        {
            Dispose(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe override Span<T> GetSpan() => new Span<T>((void*)memory, length);

        private bool IsRetained => refCount > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MemoryHandle Pin(int elementIndex = 0)
        {
            unsafe
            {
                Retain();
                if ((uint)elementIndex > length) throw new ArgumentOutOfRangeException(nameof(elementIndex));
                void* pointer = Unsafe.Add<T>((void*)memory, elementIndex);
                return new MemoryHandle(pointer, default, this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Release()
        {
            int newRefCount = Interlocked.Decrement(ref refCount);

            if (newRefCount < 0)
            {
                throw new InvalidOperationException("Unmatched Release/Retain");
            }

            return newRefCount != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Retain()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(SimpleMemoryManagerBufferWriter<T>));
            }

            Interlocked.Increment(ref refCount);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            // typically this would call into a native method appropriate for the platform
            Marshal.FreeHGlobal(memory);
            memory = IntPtr.Zero;

            IsDisposed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool TryGetArray(out ArraySegment<T> arraySegment)
        {
            // cannot expose managed array
            arraySegment = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Unpin()
        {
            Release();
        }
    }
}
