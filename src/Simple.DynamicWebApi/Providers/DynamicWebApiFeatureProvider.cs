using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace Simple.DynamicWebApi;

/// <summary>
/// 自定义控制器特性提供程序，用于发现动态API控制器
/// </summary>
internal class DynamicWebApiFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
    {
        return ControllerSelector.IsDynamicWebApiController(typeInfo);
    }
}
