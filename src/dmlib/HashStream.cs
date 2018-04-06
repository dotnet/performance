using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace AzDataMovementTest
{
    internal class HashStream : Stream
    {
        public override bool CanRead {get;} = false;
        public override bool CanSeek {get;} = true;
        public override bool CanTimeout {get;} = false;
        public override bool CanWrite { get;} = true;
        public override long Position {
          get { return position; }
          set {
                if (value < frontier) {
                    throw new InvalidOperationException("Cannot seek to before commited data");
                }
                else {
                    position = value;
                }
            }
        }

        public override long Length { get {return length;} }

        private HashAlgorithm hasher = null;
        private bool finalized = false;
        private long frontier = 0; // The position up to which data has already been commited and discarded
        private long position = 0; // The current write position
        private long length = 0; // The current write position
        private BlockList uncommited = new BlockList();

        public HashStream()
        {
            hasher = MD5.Create();
        }

        public override long Seek(long pos, SeekOrigin origin) { throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (position == frontier)
            {
                Transform(buffer, offset, count);
                frontier += count;
            }
            else
            {
                var copy = new byte[count];
                Array.Copy(buffer, offset, copy, 0, count);

                uncommited.Add(new Block {
                    Start = position,
                    Data = copy
                });

                Commit();
            }
            position += count;
        }

        private void Commit()
        {
            if (uncommited.Start == frontier)
            {
                var commit = uncommited.Pull();
                if (commit != null)
                {
                    foreach (var block in commit)
                    {
                        Transform(block.Data, 0, block.Data.Length);
                        frontier += block.Size;
                    }
                }
            }
        }

        private void Transform(byte[] buffer, int offset, int count)
        {
            if (!finalized)
            {
                hasher.TransformBlock(buffer, offset, count, buffer, offset);
                length += count;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        // Finalizes and retrieves the hash
        public byte[] Hash
        {
          get {
              if (!finalized)
              {
                  Commit();
                  if ( uncommited.Count > 0 )
                  {
                      throw new InvalidOperationException("There is still uncommited data in the stream");
                  }
                  hasher.TransformFinalBlock(new byte[]{}, 0, 0); // Finalize the hash
                  finalized = true;
              }
              return hasher.Hash;
          }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hasher.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void SetLength(long len) { }
        public override int Read(byte[] buffer, int offset, int len) { throw new NotSupportedException(); }
        public override void Flush() { }
    }

    internal class BlockList
    {
        // Invariants:
        // 1. The list is sorted
        // 2. The list contains no overlapping blocks
        private List<Block> Blocks { get; set; } = new List<Block>();

        public long Start {
            get
            {
                return Blocks.Count > 0 ? Blocks[0].Start : 0;
            }
        }

        public long Size { get; private set; }
        public long Count { get { return Blocks.Count; } }

        public void Add(Block block)
        {
            var index = Blocks.BinarySearch(block);
            if (index < 0) index = ~index;
            Blocks.Insert(index, block);

            Size += block.Size;
        }

        public List<Block> Pull()
        {
            List<Block> result = null;

            int removeCount = 0;
            long removeSize = 0;
            Block last = null;

            foreach (var block in Blocks)
            {
                if (result == null)
                {
                    result = new List<Block>{ block };
                    removeCount++;
                    removeSize += block.Size;
                }
                else if (block.Start == last.End)
                {
                    result.Add(block);
                    removeCount++;
                    removeSize += block.Size;
                }
                else break;

                last = block;
            }

            Blocks.RemoveRange(0, removeCount);
            Size -= removeSize;

            return result;
        }
    }

    internal class Block : IComparable<Block>
    {
        public long Start { get; set; }
        public long End { get { return Start + Data.Length; } }
        public byte[] Data { get; set; }
        public int Size { get { return Data.Length; } }

        public int CompareTo(Block other) {
            if (other == null) return -1; // null is after every block
            return (int)(this.Start - other.Start);
        }
    }
}
