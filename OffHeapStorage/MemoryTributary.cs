using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace System.IO
{
    /// <summary>
    /// MemoryTributary is a re-implementation of MemoryStream that uses a dynamic list of natively allocated memory blocks as a backing store,
    /// instead of a single managed byte array. The blocks live outside the GC heap, so the garbage collector never scans or moves them.
    /// Native memory is only released by Dispose (or, as a fallback, the finalizer), so always dispose instances when done.
    /// </summary>
    public unsafe class MemoryTributary : Stream       /* http://msdn.microsoft.com/en-us/library/system.io.stream.aspx */
    {
        public const int DefaultBlockSize = 1 << 20;

        #region Constructors

        public MemoryTributary() : this(0, DefaultBlockSize)
        {
        }

        public MemoryTributary(byte[] source) : this(0, DefaultBlockSize)
        {
            this.Write(source, 0, source.Length);
            Position = 0;
        }

        public MemoryTributary(int length) : this(length, DefaultBlockSize)
        {
        }

        public MemoryTributary(long length, int blockSize)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException("blockSize", blockSize, "Block size must be positive.");
            this.blockSize = blockSize;
            SetLength(length);
            if (length > 0)
                Block((length - 1) / blockSize);   //prompt the allocation of memory
            Position = 0;
        }

        ~MemoryTributary()
        {
            Dispose(false);
        }

        #endregion

        #region Status Properties

        public override bool CanRead
        {
            get { return !disposed; }
        }

        public override bool CanSeek
        {
            get { return !disposed; }
        }

        public override bool CanWrite
        {
            get { return !disposed; }
        }

        #endregion

        #region Public Properties

        public override long Length
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                return length;
            }
        }

        public override long Position { get; set; }

        /// <summary>
        /// The number of bytes of native memory currently allocated by this stream
        /// </summary>
        public long AllocatedBytes
        {
            get { return (long)blocks.Count * blockSize; }
        }

        #endregion

        #region Members

        protected long length = 0;

        protected readonly int blockSize;

        protected readonly List<IntPtr> blocks = new List<IntPtr>();

        private bool disposed;

        #endregion

        #region Internal Properties

        /// <summary>
        /// Returns a pointer to the given block, allocating (zeroed) blocks up to and including it as needed
        /// </summary>
        protected byte* Block(long blockId)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            while (blocks.Count <= blockId)
                blocks.Add((IntPtr)NativeMemory.AllocZeroed((nuint)blockSize));
            return (byte*)blocks[(int)blockId];
        }

        #endregion

        #region Public Stream Methods

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            int toRead = (int)Math.Min(buffer.Length, Math.Max(0, length - Position));
            int read = 0;
            while (read < toRead)
            {
                int blockOffset = (int)(Position % blockSize);
                int copysize = Math.Min(toRead - read, blockSize - blockOffset);
                new ReadOnlySpan<byte>(Block(Position / blockSize) + blockOffset, copysize).CopyTo(buffer.Slice(read));
                read += copysize;
                Position += copysize;
            }
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", value, "Length cannot be negative.");

            if (value < length)
            {
                // Free whole blocks past the new end and zero the tail of the last one, so growing again reads zeros
                long neededBlocks = (value + blockSize - 1) / blockSize;
                for (int i = blocks.Count - 1; i >= neededBlocks; i--)
                {
                    NativeMemory.Free((void*)blocks[i]);
                    blocks.RemoveAt(i);
                }
                int tailOffset = (int)(value % blockSize);
                if (tailOffset != 0 && blocks.Count > 0)
                    new Span<byte>((byte*)blocks[blocks.Count - 1] + tailOffset, blockSize - tailOffset).Clear();
            }
            length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            long initialPosition = Position;
            try
            {
                int written = 0;
                while (written < buffer.Length)
                {
                    int blockOffset = (int)(Position % blockSize);
                    int copysize = Math.Min(buffer.Length - written, blockSize - blockOffset);
                    buffer.Slice(written, copysize).CopyTo(new Span<byte>(Block(Position / blockSize) + blockOffset, copysize));
                    written += copysize;
                    Position += copysize;
                }
            }
            catch
            {
                Position = initialPosition;
                throw;
            }
            EnsureCapacity(Position);
        }

        public override int ReadByte()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (Position >= length)
                return -1;

            byte b = Block(Position / blockSize)[Position % blockSize];
            Position++;

            return b;
        }

        public override void WriteByte(byte value)
        {
            Block(Position / blockSize)[Position % blockSize] = value;
            Position++;
            EnsureCapacity(Position);
        }

        protected void EnsureCapacity(long intended_length)
        {
            if (intended_length > length)
                length = (intended_length);
        }

        #endregion

        #region IDispose

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                foreach (var block in blocks)
                    NativeMemory.Free((void*)block);
                blocks.Clear();
                length = 0;
                disposed = true;
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Public Additional Helper Methods

        /// <summary>
        /// Returns the entire content of the stream as a byte array. This is not safe because the call to new byte[] may
        /// fail if the stream is large enough. Where possible use methods which operate on streams directly instead.
        /// </summary>
        /// <returns>A byte[] containing the current data in the stream</returns>
        public byte[] ToArray()
        {
            long firstposition = Position;
            Position = 0;
            byte[] destination = new byte[Length];
            ReadExactly(destination, 0, (int)Length);
            Position = firstposition;
            return destination;
        }

        /// <summary>
        /// Reads length bytes from source into the this instance at the current position.
        /// </summary>
        /// <param name="source">The stream containing the data to copy</param>
        /// <param name="length">The number of bytes to copy</param>
        public void ReadFrom(Stream source, long length)
        {
            byte[] buffer = new byte[4096];
            int read;
            while (length > 0)
            {
                read = source.Read(buffer, 0, (int)Math.Min(4096, length));
                if (read == 0)
                    break;
                length -= read;
                this.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Writes the entire stream into destination, regardless of Position, which remains unchanged.
        /// </summary>
        /// <param name="destination">The stream to write the content of this stream to</param>
        public void WriteTo(Stream destination)
        {
            long initialpos = Position;
            Position = 0;
            this.CopyTo(destination);
            Position = initialpos;
        }

        #endregion
    }
}
