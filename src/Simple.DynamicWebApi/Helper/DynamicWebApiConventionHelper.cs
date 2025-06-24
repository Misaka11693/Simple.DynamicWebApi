using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Simple.DynamicWebApi;

/// <summary>
/// DynamicWebApiConventionHelper 静态帮助类。
/// </summary>
internal class DynamicWebApiConventionHelper
{
    private DynamicWebApiSettingsOptions dynamicWebApiOptions;

    public DynamicWebApiConventionHelper(DynamicWebApiSettingsOptions dynamicWebApiOptions)
    {
        this.dynamicWebApiOptions = dynamicWebApiOptions;
    }


    /// <summary>
    /// 根据Action名称选择HttpMethod
    /// </summary>
    internal string SelectHttpMethod(ActionModel action)
    {
        foreach (var conventionalPrefix in dynamicWebApiOptions.ConventionalPrefixes)
        {
            if (conventionalPrefix.Value.Any(prefix => action.ActionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return conventionalPrefix.Key;
            }
        }
        return dynamicWebApiOptions.DefaultHttpMethod;
    }


    /// <summary>
    /// 获取控制器根路径
    /// </summary>
    /// <param name="controllerType"></param>
    /// <returns></returns>
    internal string GetRootPathOrDefault(Type controllerType)
    {
        //1从[Area]特性中获取
        var areaAttribute = controllerType.GetCustomAttributes().OfType<AreaAttribute>().FirstOrDefault();
        if (areaAttribute?.RouteValue != null)
        {
            return areaAttribute.RouteValue;
        }

        //2.从dynamicWebApiOptions中获取
        return dynamicWebApiOptions.DefaultRootPath;
    }



    /// <summary>
    /// 验证选择器约束规则，确保HTTP方法配置符合规范
    /// </summary>
    internal void ValidateSelectorConstraints(ActionModel action)
    {
        //  检查整个动作是否有多个选择器定义了不同的HTTP方法
        var httpMethods = action.Selectors
            .SelectMany(s => s.ActionConstraints.OfType<HttpMethodActionConstraint>())
            .SelectMany(c => c.HttpMethods)
            .ToList();

        // 一个动作只能对应一种HTTP方法
        if (httpMethods.Count > 1)
        {
            // 抛出异常
            throw new InvalidOperationException($"Action '{action.ActionName}' in controller '{action.Controller.ControllerType.FullName}' has multiple HTTP methods defined: {string.Join(", ", httpMethods)}.");
        }
    }

    /// <summary>
    /// 获取默认路由模板
    /// </summary>
    /// <returns></returns>
    internal string GetDefaultRouteTemplate()
    {
        var segments = new List<string>();

        if (dynamicWebApiOptions.AddRoutePrefixToRoute)
        {
            segments.Add("[api]");
        }

        if (dynamicWebApiOptions.AddRootPathToRoute)
        {
            segments.Add("[rootPath]");
        }

        //[api]/[rootPath]/[controller]/[action]
        segments.AddRange(new string[] { "[controller]", "[action]" });
        return string.Join("/", segments);
    }

    internal string GetRoutePrefixOrDefault()
    {
        return dynamicWebApiOptions.DefaultRoutePrefix;
    }


}
