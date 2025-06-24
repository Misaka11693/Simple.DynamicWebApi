using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.DynamicWebApi;

internal static class CollectionHelper
{
    /// <summary>
    /// 判断集合是否为空或null
    /// </summary>
    internal static bool IsNullOrEmpty<T>(this ICollection<T>? source)
    {
        return source == null || source.Count <= 0;
    }
}
