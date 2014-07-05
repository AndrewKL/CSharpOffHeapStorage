using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OffHeapStorage
{
    public class ObjectSerializationInfo
    {
        public string Name { get; set; }
        public Func<object> Constructor { get; set; }
        public List<PropertySerializationInfo> PropertyList { get; set; }

        public ObjectSerializationInfo(Type type)
        {
            var props = type.GetTypeInfo().DeclaredProperties.Where(x =>
                   x.PropertyType == typeof(int)
                || x.PropertyType == typeof(string)
                || x.PropertyType == typeof(decimal)
                || x.PropertyType == typeof(float)
                || x.PropertyType == typeof(bool)).ToList();

            PropertyList = new List<PropertySerializationInfo>();

            foreach (var prop in props)
            {
                PropertyList.Add(new PropertySerializationInfo()
                {
                    PropertyInfo = prop,
                    Getter = BuildGetterLambda(prop,type),
                    Setter = BuildSetter(prop,type)
                });
            }
        }

        public static Func<object, object> BuildGetterLambda(PropertyInfo propertyInfo,Type objectType)
        {
            ParameterExpression arg = Expression.Parameter(typeof(object), "x");

            Expression expr = Expression.Property(Expression.Convert(arg,objectType), "Name");

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


    }
}
