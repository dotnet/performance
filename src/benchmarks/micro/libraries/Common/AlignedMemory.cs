// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    internal sealed class AlignedMemory : MemoryManager<byte>
    {
        private bool _disposed;
        private int _refCount;
        private IntPtr _memory;
        private int _length;

        private unsafe AlignedMemory(void* memory, int length)
        {
            _memory = (IntPtr)memory;
            _length = length;
        }

        public static unsafe AlignedMemory Allocate(uint length, uint alignment)
            => new AlignedMemory(NativeMemory.AlignedAlloc(length, alignment), (int)length);

        public bool IsDisposed => _disposed;

        public unsafe override System.Span<byte> GetSpan() => new System.Span<byte>((void*)_memory, _length);

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            unsafe
            {
                Retain();
                if ((uint)elementIndex > _length) throw new ArgumentOutOfRangeException(nameof(elementIndex));
                void* pointer = Unsafe.Add<byte>((void*)_memory, elementIndex);
                return new MemoryHandle(pointer, default, this);
            }
        }

        private bool Release()
        {
            int newRefCount = Interlocked.Decrement(ref _refCount);

            if (newRefCount < 0)
            {
                throw new InvalidOperationException("Unmatched Release/Retain");
            }

            return newRefCount != 0;
        }

        private void Retain()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AlignedMemory));
            }

            Interlocked.Increment(ref _refCount);
        }

        protected override unsafe void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            NativeMemory.AlignedFree(_memory.ToPointer());
            _disposed = true;
        }

        protected override bool TryGetArray(out ArraySegment<byte> arraySegment)
        {
            // cannot expose managed array
            arraySegment = default;
            return false;
        }

        public override void Unpin() => Release();
    }
}