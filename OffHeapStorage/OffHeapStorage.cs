using System.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace OffHeapStorage
{
    public class OffHeapIEnumerable<T> : IEnumerable<T>
    {
        private readonly MemoryStream _stream;
        private Serializer<T> _serializer;

        public OffHeapIEnumerable(IEnumerable<T> input)
        {
            _stream = new MemoryStream();
            _serializer = new Serializer<T>(_stream);

            foreach (var obj in input)
            {
                _serializer.Serialize<T>(_stream, obj);
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
