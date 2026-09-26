using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OffHeapStorage
{
    public class ObjectSerializationInfo
    {
        public string Name { get; set; }
        public Func<object> Constructor { get; set; }
        public List<PropertySerializationInfo> PropertyList { get; set; }

        public ObjectSerializationInfo(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite
                    && x.GetIndexParameters().Length == 0
                    && PropertySerializationInfo.MapTypeToTypeEnum.ContainsKey(x.PropertyType))
                .ToList();

            PropertyList = new List<PropertySerializationInfo>();

            foreach (var prop in props)
            {
                PropertyList.Add(new PropertySerializationInfo()
                {
                    PropertyInfo = prop,
                    Getter = BuildGetterLambda(prop,type),
                    Setter = BuildSetter(prop,type),
                    PropType = PropertySerializationInfo.MapTypeToTypeEnum[prop.PropertyType]
                });
            }
            Constructor = GetConstructor(typeInfo);
        }

        public static Func<object> GetConstructor(TypeInfo typeInfo)
        {
            var ctor = typeInfo.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new ArgumentException("Type " + typeInfo.FullName + " must have a public parameterless constructor.");
            var expr = Expression.New(ctor);

            return Expression.Lambda<Func<object>>(expr).Compile();
        } 

        public static Func<object, object> BuildGetterLambda(PropertyInfo propertyInfo,Type objectType)
        {
            ParameterExpression arg = Expression.Parameter(typeof(object), "x");

            Expression expr = Expression.Convert( Expression.Property(Expression.Convert(arg, objectType), propertyInfo.Name), typeof(object));;

            return Expression.Lambda<Func<object, object>>(expr, arg).Compile();
        }

        public static Action<object, object> BuildSetter(PropertyInfo propertyInfo, Type objectType)
        {
            ParameterExpression arg1 = Expression.Parameter(typeof(object), "obj");
            ParameterExpression arg2 = Expression.Parameter(typeof(object), "value");

            var convertedObj = Expression.Convert(arg1, objectType);
            var convertedProp = Expression.Convert(arg2, propertyInfo.PropertyType);

            var expr = Expression.Assign(Expression.Property(convertedObj, propertyInfo), convertedProp);

            return Expression.Lambda<Action<object, object>>(expr,new []{arg1,arg2}).Compile();
        }
    }

    public class PropertySerializationInfo
    {
        public PropertyInfo PropertyInfo { get; set; }
        public Action<object, object> Setter { get; set; }
        public Func<object, object> Getter { get; set; }
        public PropertySerializationTypeEnum PropType { get; set; }

        public static Dictionary<Type,PropertySerializationTypeEnum> MapTypeToTypeEnum 
            = new Dictionary<Type, PropertySerializationTypeEnum>()
            {
                {typeof(Int32),PropertySerializationTypeEnum.Int32},
                {typeof(Boolean),PropertySerializationTypeEnum.Bool},
                {typeof(decimal),PropertySerializationTypeEnum.Decimal},
                {typeof(String),PropertySerializationTypeEnum.String},
                {typeof(float),PropertySerializationTypeEnum.Float},
                {typeof(double),PropertySerializationTypeEnum.Double},
            };
    }
   
    public enum PropertySerializationTypeEnum
    {
        none, Int32,Bool,Decimal,String,Float, Double
    }
}
