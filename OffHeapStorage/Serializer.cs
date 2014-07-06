using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffHeapStorage
{
    public class Serializer<T>
    {
        private ObjectSerializationInfo serializationInfo;
        private Stream _stream;
        private BinaryWriter _binaryWriter;
        private BinaryReader _binaryReader;
        public Serializer(Stream stream)
        {
            this.serializationInfo = new ObjectSerializationInfo(typeof(T));
            _stream = stream;
            _binaryWriter = new BinaryWriter(stream);
            _binaryReader = new BinaryReader(stream);
        }


        public void Serialize(T obj)
        {
            foreach (var prop in serializationInfo.PropertyList)
            {

                switch (prop.PropType)
                {
                    case(PropertySerializationTypeEnum.Int32):
                        _binaryWriter.Write((Int32)prop.Getter(obj));
                        break;
                    case (PropertySerializationTypeEnum.Bool):
                        _binaryWriter.Write((Boolean)prop.Getter(obj));
                        break;
                    case (PropertySerializationTypeEnum.Decimal):
                        _binaryWriter.Write((decimal)prop.Getter(obj));
                        break;
                    case (PropertySerializationTypeEnum.Float):
                        _binaryWriter.Write((float)prop.Getter(obj));
                        break;
                    case (PropertySerializationTypeEnum.String):
                        _binaryWriter.Write((string)prop.Getter(obj));
                        break;
                    case (PropertySerializationTypeEnum.Double):
                        _binaryWriter.Write((double)prop.Getter(obj));
                        break;
                }
            }
        }

        public IEnumerable<T> DeserializeStream()
        {
            _stream.Position = 0;
            while (_stream.Position != _stream.Length)
            {
                yield return Deserialize();
            }
        } 
        
        private T Deserialize()
        {
            var obj = serializationInfo.Constructor();

            foreach (var prop in serializationInfo.PropertyList)
            {
                switch (prop.PropType)
                {
                    case (PropertySerializationTypeEnum.Int32):
                        prop.Setter(obj,_binaryReader.ReadInt32());
                        break;
                    case (PropertySerializationTypeEnum.Bool):
                        prop.Setter(obj, _binaryReader.ReadBoolean());
                        break;
                    case (PropertySerializationTypeEnum.Decimal):
                        prop.Setter(obj, _binaryReader.ReadDecimal());
                        break;
                    case (PropertySerializationTypeEnum.Float):
                        prop.Setter(obj, _binaryReader.ReadSingle());
                        break;
                    case (PropertySerializationTypeEnum.String):
                        prop.Setter(obj, _binaryReader.ReadString());
                        break;
                    case (PropertySerializationTypeEnum.Double):
                        prop.Setter(obj, _binaryReader.ReadDouble());
                        break;
                }
            }
            return (T) obj;

        }
    }
}
