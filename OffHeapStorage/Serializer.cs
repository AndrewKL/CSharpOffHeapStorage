using System;
using System.Collections.Generic;
using System.IO;

namespace OffHeapStorage
{
    public interface ISerializer<T>
    {
        void Serialize(T obj);
        IEnumerable<T> DeserializeStream();
    }

    public class Serializer<T> : ISerializer<T>
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
            _stream.Position = _stream.Length;
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
                        var str = (string)prop.Getter(obj);
                        _binaryWriter.Write(str != null);
                        if (str != null)
                            _binaryWriter.Write(str);
                        break;
                    case (PropertySerializationTypeEnum.Double):
                        _binaryWriter.Write((double)prop.Getter(obj));
                        break;
                }
            }
        }

        public IEnumerable<T> DeserializeStream()
        {
            // Track position per enumeration so interleaved enumerators don't corrupt each other
            long position = 0;
            while (position < _stream.Length)
            {
                _stream.Position = position;
                var obj = Deserialize();
                position = _stream.Position;
                yield return obj;
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
                        prop.Setter(obj, _binaryReader.ReadBoolean() ? _binaryReader.ReadString() : null);
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
