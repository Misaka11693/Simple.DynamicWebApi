using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Simple.DynamicWebApi;

public static class TypeHelper
{
    private static readonly HashSet<Type> PrimitiveTypes = new()
    {
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(char),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    };

    /// <summary>
    /// 判断是否是简单类型
    /// </summary>
    public static bool IsSimpleType(Type type)
    {
        if (type == null) return false;

        // 处理可空类型
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type)!;
        }

        return type.IsPrimitive ||
               PrimitiveTypes.Contains(type) ||
               type.IsEnum;
    }

    /// <summary>
    /// 判断是否是文件类型
    /// </summary>
    public static bool IsFileType(Type type)
    {
        if (type == null) return false;

        return typeof(IFormFile).IsAssignableFrom(type) ||
               typeof(IFormFileCollection).IsAssignableFrom(type);
    }


    /// <summary>
    /// 判断是否是复杂类型（需要[FromBody]绑定）
    /// </summary>
    public static bool IsComplexType(Type type)
    {
        if (type == null) return false;

        return !IsSimpleType(type) &&
               !IsFileType(type);
    }

    /// <summary>
    /// 判断是否适合作为路由参数
    /// </summary>
    public static bool IsSuitableForRoute(Type type)
    {
        if (type == null) return false;

        return IsSimpleType(type);
    }
}
