using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.DynamicWebApi;

/// <summary>
/// 动态 WebApi 控制器配置选项
/// Async后缀由Asp.Net Core MVC自动移除，例如GetAsync => Get，不可配置保留Async
/// </summary>
public sealed class DynamicWebApiSettingsOptions
{
    /// <summary>
    /// 是否启用动态WebApi(默认启用)
    /// </summary>
    public bool EnableDynamicWebApi { get; set; } = true;

    /// <summary>
    /// 默认的Http方法(默认POST)
    /// </summary>
    public string DefaultHttpMethod { get; set; } = "POST";
    /// <summary>
    /// 动态WebApi路由前缀(默认api）
    /// </summary>
    public string DefaultRoutePrefix { get; set; } = "api";

    /// <summary>
    /// 默认根路径(默认app)
    /// </summary>
    public string DefaultRootPath { get; set; } = "app";

    /// <summary>
    /// 是否添加路由前缀到路由(默认添加) api/..
    /// </summary>
    public bool AddRoutePrefixToRoute { get; set; } = true;

    /// <summary>
    /// 是否将根路径添加到路由(默认不添加) api/app/..
    /// </summary>
    public bool AddRootPathToRoute { get; set; } = false;

    /// <summary>
    /// 是否移除Action的前缀(默认移除) 例如GetUserInfo => UserInfp
    /// </summary>
    public bool RemoveActionPrefix { get; set; } = true;

    /// <summary>
    /// 是否移除Controller的后缀(默认移除）例如UsersController => Users
    /// </summary>
    public bool RemoveControllerSuffix { get; set; } = true;

    /// <summary>
    /// 需要移除的Controller名称后缀(RemoveControllerSuffix为true时生效)
    /// </summary>
    public string[] ControllerPostfixes { get; set; } = { "ApplicationService", "AppService", "AppServices", "Service", "Services", "ApiController", "Controller" };

    /// <summary>
    /// 默认的约定前缀,用于匹配Action名称与Http方法
    /// </summary>
    public Dictionary<string, string[]> ConventionalPrefixes = new Dictionary<string, string[]>
    {
        { "GET", new[] { "Get", "Query", "Find", "Fetch", "Select" } },
        { "POST", new[] { "Post", "Create", "Add", "Insert", "Submit", "Save" } },
        { "PATCH", new[] { "Patch" } },
        { "PUT", new[] { "Put", "Update" } },
        { "DELETE", new[] { "Delete", "Remove", "Clear" } }
    };
}
