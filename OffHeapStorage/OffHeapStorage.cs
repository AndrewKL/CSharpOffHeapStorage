using System.Collections.Generic;
using System.IO;

namespace OffHeapStorage
{
    public class OffHeapIEnumerable<T> : IEnumerable<T>
    {
        private readonly Stream _stream;
        private Serializer<T> _serializer;

        public OffHeapIEnumerable(IEnumerable<T> input)
        {
            _stream = new MemoryTributary();
            _serializer = new Serializer<T>(_stream);

            foreach (var obj in input)
            {
                _serializer.Serialize(obj);
            }
        }       

        public IEnumerator<T> GetEnumerator()
        {
            return _serializer.DeserializeStream().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


    }

    
}
