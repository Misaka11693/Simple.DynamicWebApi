using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Simple.DynamicWebApi.Helper;
using Simple.DynamicWebApi.Options;
using System.Reflection;

namespace Simple.DynamicWebApi.Conventions;

/// <summary>
/// 动态接口控制器应用模型转换器
/// </summary>
public class DynamicWebApiConvention : IApplicationModelConvention
{
    private readonly DynamicWebApiSettingsOptions _dynamicWebApiOptions;

    public DynamicWebApiConvention(DynamicWebApiSettingsOptions dynamicWebApiOptions)
    {
        _dynamicWebApiOptions = dynamicWebApiOptions;
    }

    /// <summary>
    /// 普通方法转换为动态接口控制器
    /// </summary>
    /// <param name="application"></param>
    public void Apply(ApplicationModel application)
    {
        //仅需对动态接口控制器进行配置
        var dynamicWebApiControllers = application.Controllers.Where(u => { return ControllerSelector.IsDynamicWebApiController(u.ControllerType); });

        foreach (var controller in dynamicWebApiControllers)
        {
            controller.ControllerName = FormatControllerName(controller);
            ConfigureDynamicWebApi(controller);
        }
    }

    /// <summary>
    /// 配置动态WebApi控制器的文档显示、路由规则、控制器参数
    /// </summary>
    /// <param name="controller"></param>
    private void ConfigureDynamicWebApi(ControllerModel controller)
    {
        ConfigureApiExplorer(controller);
        ConfigureSelector(controller);
        ConfigureParameters(controller);
    }

    /// <summary>
    /// Swagger/OpenAPI 文档分组、显示
    /// </summary>
    /// <param name="controller"></param>
    private void ConfigureApiExplorer(ControllerModel controller)
    {
        if (!controller.ApiExplorer.IsVisible.HasValue)
        {
            controller.ApiExplorer.IsVisible = true;
            controller.ApiExplorer.GroupName = controller.ControllerName;//Swagger文档分组为控制器名称,默认为控制器名称
        }

        foreach (var action in controller.Actions)
        {
            if (!action.ApiExplorer.IsVisible.HasValue)
            {
                action.ApiExplorer.IsVisible = true;
            }
        }

    }

    /// <summary>
    /// 参数配置
    /// </summary>
    /// <param name="controller"></param>
    private void ConfigureSelector(ControllerModel controller)
    {
        RemoveEmptySelectors(controller.Selectors); //每个 SelectorModel 都定义了一个 路由规则，包含
                                                    //1.路由模板（AttributeRouteModel）： [Route("api/users")]
                                                    //2.HTTP 方法约束（ActionConstraints）：[HttpGet]、[HttpPost]...
                                                    //3.端点元数据（EndpointMetadata）:[ApiController]、[Authorize]、[AllowAnonymous]、与路由相关的元数据，例如 Swagger 描述、自定义过滤器等。
                                                    // asp.net core 框架会自动生成一些空白信息，这里移除空白信息，然后实现自定义路由规则
                                                    //获取控制器根路径
        var rootPath = GetRootPathOrDefault(controller.ControllerType.AsType());

        foreach (var action in controller.Actions)
        {
            ConfigureActionSelector(controller, rootPath, controller.ControllerName, action);
        }
    }

    /// <summary>
    /// 添加路由规则
    /// </summary>
    private void ConfigureActionSelector(ControllerModel controller, string rootPath, string controllerName, ActionModel action)
    {
        RemoveEmptySelectors(action.Selectors);

        if (!action.Selectors.Any())
        {
            var selector = new SelectorModel();
            action.Selectors.Add(selector);
        }

        NormalizeSelectorRoutes(controller, rootPath, controllerName, action);
    }

    /// <summary>
    /// 移除空白信息
    /// </summary>
    protected virtual void RemoveEmptySelectors(IList<SelectorModel> selectors)
    {
        selectors
            .Where(IsEmptySelector)
            .ToList()
            .ForEach(s => selectors.Remove(s));
    }
    protected virtual bool IsEmptySelector(SelectorModel selector)
    {
        return selector.AttributeRouteModel == null
               && (selector.ActionConstraints == null || selector.ActionConstraints.Count <= 0)
               && (selector.EndpointMetadata == null || selector.EndpointMetadata.Count <= 0);
    }

    /// <summary>
    /// 获取控制器根路径
    /// </summary>
    /// <param name="controllerType"></param>
    /// <returns></returns>
    protected virtual string GetRootPathOrDefault(Type controllerType)
    {
        //1从[Area]特性中获取
        var areaAttribute = controllerType.GetCustomAttributes().OfType<AreaAttribute>().FirstOrDefault();
        if (areaAttribute?.RouteValue != null)
        {
            return areaAttribute.RouteValue;
        }

        //2.从dynamicWebApiOptions中获取
        return _dynamicWebApiOptions.DefaultRootPath;
    }

    /// <summary>
    /// 实现自定义路由规则
    /// </summary>
    private void NormalizeSelectorRoutes(ControllerModel controller, string rootPath, string controllerName, ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            var httpMethod = selector.ActionConstraints
                                .OfType<HttpMethodActionConstraint>()
                                .FirstOrDefault()?
                                .HttpMethods?
                                .FirstOrDefault();

            if (httpMethod == null)
            {
                httpMethod = DynamicWebApiHelper.SelectHttpMethod(action.ActionName, _dynamicWebApiOptions.ConventionalPrefixes) ?? _dynamicWebApiOptions.DefaultHttpMethod;
            }

            // 如果没有定义 HTTP 方法约束，则添加默认的 HTTP 方法,如当Action打上[HttpGet]特性时，此处会自动添加[HttpGet]约束
            if (!selector.ActionConstraints.OfType<HttpMethodActionConstraint>().Any())
            {
                selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { httpMethod }));
            }


            // 如果action已存在[Route]特性， ASP.NET Core 会自动对其进行路由模板的处理，此处需要清空掉ASP.NET Core 自动生成的路由模板
            // 通过类型判断，不能通过是否为空来判断：因为如果是存在[HttpGet]特性，selector.AttributeRouteModel 不为空
            if (selector.AttributeRouteModel?.Attribute is AttributeRouteModel)
            {
                //selector.AttributeRouteModel.Template = string.Empty;
                //改为报错更清晰
                throw new InvalidOperationException($"控制器 '{controller.ControllerType.FullName}' 中的方法 '{action.ActionName}' 具有 [Route] 特性，DynamicWebApi 不支持此特性。");
            }

            var controllerTemplate = controller.ControllerType.GetCustomAttributes().OfType<RouteAttribute>().FirstOrDefault()?.Template;

            //情况1：controllerTemplate为空，即控制器无[Route]特性
            if (string.IsNullOrEmpty(controllerTemplate))
            {
                //情况1.1：控制器无[Route]特性,action存在[HttpMethod]特性
                if (selector.AttributeRouteModel?.Attribute is Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute)
                {
                    //对 [HttpGet("")]的情况做报错处理
                    if (selector.AttributeRouteModel.Template.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException($"控制器 '{controller.ControllerType.FullName}' 中的方法 '{action.ActionName}' 具有 [HttpGet] 特性，但没有指定路由模板。");
                    }
                    //路由模版为：api/rootPath/controllerName/{selector.AttributeRouteModel.Template}
                    selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(CalculateRouteTemplate(rootPath, controllerName, selector.AttributeRouteModel.Template!)));
                    continue;
                }
                else
                {
                    //情况1.2：控制器无[Route]特性,action不存在[HttpMethod]特性
                    selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(CalculateRouteTemplate(rootPath, controllerName, FormatActionName(action.ActionName, httpMethod))));
                    continue;
                }
            }
            else
            {

                // 情况2：控制器存在[Route]特性

                // 情况2.1：控制器存在[Route]特性,且以[action]结尾
                if (controllerTemplate.EndsWith("/[action]"))
                {
                    //[Route("api/[controller]/[action]")]
                    //对路由模版置空，避免造成重复拼接问题，如antion（CreateUser）存在[HttpGet("CreateUser")]，会造成重复拼接问题：api/users/create-user/CreateUser
                    if (selector.AttributeRouteModel?.Attribute is Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute && !selector.AttributeRouteModel.Template.IsNullOrEmpty())
                    {
                        selector.AttributeRouteModel.Template = string.Empty;
                    }

                    continue;
                }
                else
                {
                    //情况2.1：控制器存在[Route]特性,action存在[HttpMethod]特性
                    if (selector.AttributeRouteModel?.Attribute is Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute)
                    {
                        //对 [HttpGet("")]的情况做报错处理
                        if (selector.AttributeRouteModel.Template.IsNullOrEmpty())
                        {
                            throw new InvalidOperationException($"控制器 '{controller.ControllerType.FullName}' 中的方法 '{action.ActionName}' 具有 [HttpGet] 特性，但没有指定路由模板。");
                        }
                        //路由模版为：api/rootPath/controllerName/{selector.AttributeRouteModel.Template}
                        selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(selector.AttributeRouteModel.Template!));
                        continue;
                    }
                    else
                    {
                        //情况2.2：控制器存在[Route]特性,action不存在[HttpMethod]特性
                        selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(FormatActionName(action.ActionName, httpMethod)));
                        continue;
                    }
                }
            }
        }
    }

    /// <summary>
    ///  计算路由模板，例如：api/v1/users/create-user
    /// </summary>
    private string CalculateRouteTemplate(string rootPath, string controllerName, string templateName)
    {
        var url = string.Empty;

        // 1. 添加路由前缀
        if (_dynamicWebApiOptions.AddRoutePrefixToRoute)
        {
            url = $"{_dynamicWebApiOptions.DefaultRoutePrefix}";
        }

        // 2.添加根路径信息
        if (_dynamicWebApiOptions.AddRootPathToRoute)
        {
            url = url.IsNullOrEmpty() ? rootPath : $"{url}/{rootPath}";
        }

        // 3.添加控制器名称
        var controllerSegment = controllerName.ToKebabCase();

        url = url.IsNullOrEmpty() ? controllerSegment : $"{url}/{controllerSegment}";

        // 4.添加模板名称(当Action存在[HttpGet("create-user")]特性时，此处即为"create-user";如果Action不存在[HttpGet("create-user")]特性时，且名称为Get，Post等谓词，并且设置为不保留谓词前缀，此处的templateName会为空)
        if (!templateName.IsNullOrEmpty())
        {
            url = $"{url}/{templateName}";
        }

        return url;
    }

    /// <summary>
    /// 配置参数绑定信息，将非基础类型参数设置为FromBody绑定
    /// </summary>
    /// <param name="controller"></param>
    private void ConfigureParameters(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            foreach (var prm in action.Parameters)
            {
                if (prm.BindingInfo != null)
                {
                    continue;
                }

                // 非基础类型参数设置为FromBody绑定
                if (!DynamicWebApiHelper.IsPrimitiveExtendedIncludingNullable(prm.ParameterInfo.ParameterType))
                {
                    if (CanUseFormBodyBinding(action, prm))
                    {
                        prm.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
                    }
                }
            }
        }
    }

    /// <summary>
    /// 判断是否可以使用FromBody绑定
    /// </summary>
    protected virtual bool CanUseFormBodyBinding(ActionModel action, ParameterModel parameter)
    {
        if (parameter.ParameterName == "id")
        {
            return false;
        }

        foreach (var selector in action.Selectors)
        {
            if (selector.ActionConstraints == null)
            {
                continue;
            }

            foreach (var actionConstraint in selector.ActionConstraints)
            {
                var httpMethodActionConstraint = actionConstraint as HttpMethodActionConstraint;
                if (httpMethodActionConstraint == null)
                {
                    continue;
                }

                if (httpMethodActionConstraint.HttpMethods.All(hm => hm.IsIn("GET", "DELETE", "TRACE", "HEAD")))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 格式化controller名称（移除Controller后缀），例如UsersController => Users
    /// </summary>
    private string FormatControllerName(ControllerModel controller)
    {
        var controllerName = controller.ControllerName;
        if (_dynamicWebApiOptions.RemoveControllerSuffix)
        {
            controllerName = controllerName.RemovePostFix(_dynamicWebApiOptions.ControllerPostfixes);
        }
        return controllerName;
    }

    /// <summary>
    /// 格式化Action名称（移除HTTP方法前缀），例如GetUsersAsync => users, PostCreateUserAsync => create-user
    /// Async后缀由Asp.Net Core MVC自动移除，此处不再处理
    /// </summary>
    /// <param name="actionName"></param>
    /// <param name="httpMethod"></param>
    /// <returns></returns>
    private string FormatActionName(string actionName, string httpMethod)
    {
        if (_dynamicWebApiOptions.RemoveActionPrefix)
        {
            actionName = actionName.RemoveHttpMethodPrefix(httpMethod, _dynamicWebApiOptions.ConventionalPrefixes);
        }

        return actionName.ToKebabCase();
    }
}
