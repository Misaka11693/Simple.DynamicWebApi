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
    /// 判断是否是可解析类型（支持Parse/TryParse方法）
    /// </summary>
    public static bool IsParsableType(Type type)
    {
        if (type == null) return false;

        // 检查Parse方法
        if (type.GetMethod("Parse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null) != null)
        {
            return true;
        }

        // 检查TryParse方法
        var tryParseMethod = type.GetMethod("TryParse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), type.MakeByRefType() },
            null);

        return tryParseMethod != null && tryParseMethod.ReturnType == typeof(bool);
    }

    /// <summary>
    /// 判断是否是复杂类型（需要[FromBody]绑定）
    /// </summary>
    public static bool IsComplexType(Type type)
    {
        if (type == null) return false;

        return !IsSimpleType(type) &&
               !IsFileType(type) &&
               !IsParsableType(type);
    }

    /// <summary>
    /// 判断是否适合作为路由参数
    /// </summary>
    public static bool IsSuitableForRoute(Type type)
    {
        if (type == null) return false;

        return IsSimpleType(type) || IsParsableType(type);
    }
}
