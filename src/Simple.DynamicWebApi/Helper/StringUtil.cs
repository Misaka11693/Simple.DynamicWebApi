using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Simple.DynamicWebApi;

internal static class StringUtil
{
    /// <summary>
    /// 移除字符串末尾的指定后缀（默认区分大小写），如:TestAppService -> Test
    /// </summary>
    internal static string RemovePostFix(this string str, StringComparison comparisonType = StringComparison.Ordinal, params string[] postfixes)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        if (postfixes == null || postfixes.Count() == 0 )
        {
            return str;
        }
        
        foreach (var postFix in postfixes.Where(pf => !string.IsNullOrEmpty(pf)))
        {
            if (str.EndsWith(postFix, comparisonType))
            {
                return str.Substring(0, str.Length - postFix.Length);
            }
        }

        return str;
    }

    /// <summary>
    /// 移除字符串开头的指定前缀（默认区分大小写），如:GetUserInfo -> UserInfo
    /// </summary>
    internal static string RemovePreFix(this string str, StringComparison comparisonType = StringComparison.Ordinal, params string[] preFixes)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        if (preFixes == null || preFixes.Count() == 0)
        {
            return str;
        }

        foreach (var preFix in preFixes.Where(pf => !string.IsNullOrEmpty(pf)))
        {
            if (str.StartsWith(preFix, comparisonType))
            {
                return str.Substring(preFix.Length);
            }
        }

        return str;
    }

    /// <summary>
    /// 转Kebab命名法（如 "HelloWorld" → "hello-world"）
    /// </summary>
    internal static string ToKebabCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        str = str.ToCamelCase();

        return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + "-" + char.ToLowerInvariant(m.Value[1]));
    }

    /// <summary>
    /// 转换为驼峰命名（首字母小写，如 "Test" → "test"，"HelloWorld" → "helloWorld"）
    /// </summary>
    internal static string ToCamelCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;

        return str.Length == 1
            ? str.ToLowerInvariant()
            : char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// 判断字符串是否在给定的数据中
    /// </summary>
    internal static bool IsIn(this string str, params string[] data)
    {
        foreach (var item in data)
        {
            if (str == item)
            {
                return true;
            }
        }
        return false;
    }
}
