//------------------------------------------------------------------------------
// <copyright file="RandomStream.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace AzDataMovementBenchmark
{
    internal class RandomStream : Stream
    {
        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanTimeout { get; } = false;
        public override bool CanWrite { get; } = false;
        public override long Position { get => this.position; set { throw new NotSupportedException(); } }
        public override long Length { get => this.length; }
        
        public byte[] Hash {
            get 
            {
                if (!this.finalized)
                {
                    hasher.TransformFinalBlock(new byte[]{}, 0, 0); // Finalize the hash
                    this.finalized = true;
                }
                return hasher.Hash;
            }
        }

        private long position = 0;
        private long length = 0;
        private Random generator = new Random();
        private HashAlgorithm hasher = null;
        private bool finalized = false;

        public RandomStream(long len)
        {
            this.length = len;
            this.hasher = MD5.Create();
        }

        public override long Seek(long pos, SeekOrigin origin) { return 0; }

        public override int Read(byte[] buffer, int offset, int len)
        {
            if (this.finalized)
            {
                throw new InvalidOperationException("Cannot read from this stream after the hash is finalized");
            }
          
            // Take the min
            len = len < this.length - this.position ? len : (int)(this.length - this.position);
            
            Debug.Assert(len <= buffer.Length);
            Debug.Assert(offset >= 0);
            
            if (len > 0)
            {
                if (buffer.Length == len && offset == 0)
                {
                    generator.NextBytes(buffer);
                }
                else
                {
                    var temp = new byte[len];
                    generator.NextBytes(temp);
                    Array.Copy(temp, 0, buffer, offset, len);
                }
                
                hasher.TransformBlock(buffer, offset, len, buffer, offset);
                this.position += len;
            }
            return len;
        }
        
        public override void SetLength(long len)
        {
            this.length = len;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (hasher != null)
                {
                    hasher.Dispose();
                    hasher = null;
                }
            }

            base.Dispose(disposing);
        }

        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        public override void Flush() { }
    }
}
