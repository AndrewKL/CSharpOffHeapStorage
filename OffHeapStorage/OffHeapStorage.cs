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
        //private BinaryFormatter _binaryFormatter;

        public OffHeapIEnumerable(IEnumerable<T> input)
        {
            _stream = new MemoryStream();

            //var spec = RuntimeTypeModel.Default.Add(typeof(T), true);
            //var props = typeof(T).GetTypeInfo().DeclaredProperties.Where(x =>
            //    x.PropertyType == typeof (int)
            //    || x.PropertyType == typeof (string)
            //    || x.PropertyType == typeof (decimal)).ToList();

            //for(var i = 0;i<props.Count;i++ )
            //{
            //    spec.Add(i+1, props[i].Name);
            //}
            Serializer.Serialize<IEnumerable<T>>(_stream, input);
        }       

        public IEnumerator<T> GetEnumerator()
        {
            _stream.Position = 0;
            return Serializer.Deserialize<IEnumerator<T>>(_stream);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
