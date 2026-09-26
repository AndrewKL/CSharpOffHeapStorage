using System;
using System.Collections.Generic;
using System.IO;

namespace OffHeapStorage
{
    /// <summary>
    /// Stores a sequence serialized into natively allocated memory. Dispose to release that memory.
    /// </summary>
    public class OffHeapIEnumerable<T> : IEnumerable<T>, IDisposable
    {
        private readonly MemoryTributary _stream;
        private ISerializer<T> _serializer;

        public OffHeapIEnumerable(IEnumerable<T> input)
            : this(input, stream => new Serializer<T>(stream))
        {
        }

        protected OffHeapIEnumerable(IEnumerable<T> input, Func<Stream, ISerializer<T>> createSerializer)
        {
            _stream = new MemoryTributary();
            _serializer = createSerializer(_stream);

            foreach (var obj in input)
            {
                _serializer.Serialize(obj);
            }
        }

        /// <summary>
        /// Serialized size of the stored data, in bytes
        /// </summary>
        public long ByteCount
        {
            get { return _stream.Length; }
        }

        /// <summary>
        /// Native memory reserved for the stored data, in bytes
        /// </summary>
        public long AllocatedBytes
        {
            get { return _stream.AllocatedBytes; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _serializer.DeserializeStream().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

    }

    /// <summary>
    /// OffHeapIEnumerable using TypedSerializer, which reads and writes whole objects without boxing
    /// </summary>
    public class TypedOffHeapIEnumerable<T> : OffHeapIEnumerable<T>
    {
        public TypedOffHeapIEnumerable(IEnumerable<T> input)
            : base(input, stream => new TypedSerializer<T>(stream))
        {
        }
    }

    
}
