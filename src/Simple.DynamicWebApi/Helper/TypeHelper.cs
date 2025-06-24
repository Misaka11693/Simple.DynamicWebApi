using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Simple.DynamicWebApi;

public static class TypeHelper
{
    /// <summary>
    /// 判断类型是否为扩展原始类型（包括枚举），包含可为空的情况。
    /// </summary>
    internal static bool IsPrimitiveExtendedIncludingNullable(Type type, bool includeEnums = false)
    {
        if (IsPrimitiveExtended(type, includeEnums))
        {
            return true;
        }

        if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return IsPrimitiveExtended(type.GenericTypeArguments[0], includeEnums);
        }

        return false;
    }

    /// <summary>
    /// 判断类型是否为扩展原始类型（包括枚举）。
    /// </summary>
    internal static bool IsPrimitiveExtended(Type type, bool includeEnums)
    {
        if (type.GetTypeInfo().IsPrimitive)
        {
            return true;
        }

        if (includeEnums && type.GetTypeInfo().IsEnum)
        {
            return true;
        }

        return type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid);
    }
}
