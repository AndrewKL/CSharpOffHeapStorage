using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OffHeapStorage
{
    /// <summary>
    /// Serializer that compiles one reader and one writer per type, so values are never boxed and there is no
    /// per-property delegate call or type switch. Produces the same byte format as Serializer.
    /// </summary>
    public class TypedSerializer<T> : ISerializer<T>
    {
        /// <summary>
        /// Compiled once per T, on first use
        /// </summary>
        private static class Compiled
        {
            public static readonly Func<BinaryReader, T> Read = BuildReader(ObjectSerializationInfo.GetSerializableProperties(typeof(T)));
            public static readonly Action<BinaryWriter, T> Write = BuildWriter(ObjectSerializationInfo.GetSerializableProperties(typeof(T)));
        }

        private readonly Func<BinaryReader, T> _read;
        private readonly Action<BinaryWriter, T> _write;
        private readonly Stream _stream;
        private readonly BinaryWriter _binaryWriter;
        private readonly BinaryReader _binaryReader;

        public TypedSerializer(Stream stream)
        {
            if (typeof(T).GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException("Type " + typeof(T).FullName + " must have a public parameterless constructor.");

            _read = Compiled.Read;
            _write = Compiled.Write;
            _stream = stream;
            _binaryWriter = new BinaryWriter(stream);
            _binaryReader = new BinaryReader(stream);
        }

        public void Serialize(T obj)
        {
            _stream.Position = _stream.Length;
            _write(_binaryWriter, obj);
        }

        public IEnumerable<T> DeserializeStream()
        {
            // Track position per enumeration so interleaved enumerators don't corrupt each other
            long position = 0;
            while (position < _stream.Length)
            {
                _stream.Position = position;
                var obj = _read(_binaryReader);
                position = _stream.Position;
                yield return obj;
            }
        }

        // Compiles to: reader => new T { A = reader.ReadInt32(), B = reader.ReadBoolean() ? reader.ReadString() : null, ... }
        private static Func<BinaryReader, T> BuildReader(IEnumerable<PropertyInfo> props)
        {
            var reader = Expression.Parameter(typeof(BinaryReader), "reader");
            var bindings = props.Select(p => Expression.Bind(p, ReadValue(reader, p.PropertyType)));
            var body = Expression.MemberInit(Expression.New(typeof(T)), bindings);   // bindings run in order
            return Expression.Lambda<Func<BinaryReader, T>>(body, reader).Compile();
        }

        private static Expression ReadValue(Expression reader, Type type)
        {
            if (type == typeof(string))
                return Expression.Condition(
                    Expression.Call(reader, nameof(BinaryReader.ReadBoolean), null),
                    Expression.Call(reader, nameof(BinaryReader.ReadString), null),
                    Expression.Constant(null, typeof(string)));

            var method = type == typeof(int) ? nameof(BinaryReader.ReadInt32)
                       : type == typeof(bool) ? nameof(BinaryReader.ReadBoolean)
                       : type == typeof(decimal) ? nameof(BinaryReader.ReadDecimal)
                       : type == typeof(float) ? nameof(BinaryReader.ReadSingle)
                       : type == typeof(double) ? nameof(BinaryReader.ReadDouble)
                       : throw new NotSupportedException("Unsupported property type " + type);
            return Expression.Call(reader, method, null);
        }

        // Compiles to: (writer, obj) => { writer.Write(obj.A); var b = obj.B; writer.Write(b != null); if (b != null) writer.Write(b); ... }
        private static Action<BinaryWriter, T> BuildWriter(IEnumerable<PropertyInfo> props)
        {
            var writer = Expression.Parameter(typeof(BinaryWriter), "writer");
            var obj = Expression.Parameter(typeof(T), "obj");
            var statements = props.Select(p => WriteValue(writer, Expression.Property(obj, p)))
                                  .DefaultIfEmpty(Expression.Empty());
            return Expression.Lambda<Action<BinaryWriter, T>>(Expression.Block(statements), writer, obj).Compile();
        }

        private static Expression WriteValue(Expression writer, Expression value)
        {
            // Expression.Call picks the BinaryWriter.Write overload from the value's static type, so nothing is boxed
            if (value.Type != typeof(string))
                return Expression.Call(writer, nameof(BinaryWriter.Write), null, value);

            var str = Expression.Variable(typeof(string), "str");
            var hasValue = Expression.NotEqual(str, Expression.Constant(null, typeof(string)));
            return Expression.Block(
                new[] { str },
                Expression.Assign(str, value),
                Expression.Call(writer, nameof(BinaryWriter.Write), null, hasValue),
                Expression.IfThen(hasValue, Expression.Call(writer, nameof(BinaryWriter.Write), null, str)));
        }
    }
}
