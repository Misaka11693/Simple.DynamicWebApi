using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.DynamicWebApi;

/// <summary>
/// 动态 WebApi 控制器标记特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DynamicApiAttribute : Attribute
{
}
