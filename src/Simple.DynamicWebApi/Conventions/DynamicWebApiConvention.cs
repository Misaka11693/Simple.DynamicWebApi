using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Simple.DynamicWebApi;

/// <summary>
/// 动态接口控制器应用模型转换器
/// </summary>
public partial class DynamicWebApiConvention : IApplicationModelConvention
{
    private readonly DynamicWebApiSettingsOptions _options;

    public DynamicWebApiConvention(DynamicWebApiSettingsOptions dynamicWebApiOptions)
    {
        _options = dynamicWebApiOptions ?? throw new ArgumentNullException(nameof(dynamicWebApiOptions));
    }

    public void Apply(ApplicationModel application)
    {
        // 筛选出动态WebAPI控制器
        var dynamicWebApiControllers = application.Controllers.Where(c => { return ControllerSelector.IsDynamicWebApiController(c.ControllerType); });

        foreach (var controller in dynamicWebApiControllers)
        {
            ConfigureDynamicWebApi(controller);
        }
    }

    private void ConfigureDynamicWebApi(ControllerModel controller)
    {
        ConfigureApiExplorer(controller);
        ConfigureController(controller);
    }

    private void ConfigureApiExplorer(ControllerModel controller)
    {
        controller.ApiExplorer.IsVisible ??= true;
        controller.ApiExplorer.GroupName ??= controller.ControllerName;//Swagger文档分组为控制器名称,默认为控制器名称

        foreach (var action in controller.Actions)
        {
            action.ApiExplorer.IsVisible ??= true;
        }
    }

    private void ConfigureController(ControllerModel controller)
    {
        RemoveEmptySelectors(controller.Selectors);
        ConfigureControllerName(controller);
        ConfigureControllerRouteAttributes(controller);
        foreach (var action in controller.Actions)
        {
            ConfigureAction(action);
        }
    }

    private void ConfigureAction(ActionModel action)
    {
        ValidateAction(action);
        RemoveEmptySelectors(action.Selectors);
        ConfigureActionSelectors(action);
        ConfigureActionHttpMethodAttribute(action);
        ConfigureActionName(action);
        ConfigureComplexParameterBinding(action);
        ConfigureActionRouteAttribute(action);
    }



    internal void RemoveEmptySelectors(IList<SelectorModel> selectors)
    {
        for (int i = selectors.Count - 1; i >= 0; i--)
        {
            var selector = selectors[i];
            if (selector.AttributeRouteModel == null
                && (selector.ActionConstraints == null || selector.ActionConstraints.Count <= 0)
                && (selector.EndpointMetadata == null || selector.EndpointMetadata.Count <= 0))
            {
                selectors.RemoveAt(i);
            }
        }
    }

    private void ConfigureControllerName(ControllerModel controller)
    {
        if (_options.RemoveControllerSuffix)
        {
            controller.ControllerName = controller.ControllerName
                .RemovePostFix(postfixes: _options.ControllerPostfixes);
        }
        controller.ControllerName = controller.ControllerName.ToKebabCase();
    }

    private void ConfigureControllerRouteAttributes(ControllerModel controller)
    {
    }

    private void ValidateAction(ActionModel action)
    {
        //TODO:是否允许存在多个路由特性与HttpMethod特性？

        if (action.Attributes.Any(a => a is RouteAttribute))
        {
            throw new InvalidOperationException($"针对控制器类型‘{action.Controller.ControllerType}’上的操作‘{action.ActionName}’，不允许存在路由特性。");
        }

        if (action.Selectors.Count(s => s.ActionConstraints.OfType<HttpMethodActionConstraint>().Any()) > 1)
        {
            throw new InvalidOperationException($"针对控制器类型‘{action.Controller.ControllerType}’上的操作‘{action.ActionName}’，不允许存在多个HTTP方法。");
        }
    }

    private void ConfigureActionSelectors(ActionModel action)
    {
        if (!action.Selectors.Any())
        {
            action.Selectors.Add(new SelectorModel());
        }
    }

    private void ConfigureActionHttpMethodAttribute(ActionModel action)
    {
        var selector = action.Selectors.First();

        var existingConstraint = selector.ActionConstraints
            .FirstOrDefault(a => a is HttpMethodActionConstraint) as HttpMethodActionConstraint;

        bool hasValidConstraint = existingConstraint != null &&
                                 existingConstraint.HttpMethods != null &&
                                 existingConstraint.HttpMethods.Any();

        if (hasValidConstraint)
        {
            return; // 已有有效约束则退出
        }

        // 移除无效约束
        if (existingConstraint != null)
        {
            selector.ActionConstraints.Remove(existingConstraint);
        }

        var httpMethod = SelectHttpMethod(action);
        selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { httpMethod }));
    }

    private void ConfigureActionName(ActionModel action)
    {
        var actionNameAttribute = action.Attributes
            .OfType<ActionNameAttribute>()
            .FirstOrDefault();
        if (actionNameAttribute?.Name != null)
        {
            action.ActionName = actionNameAttribute.Name;
            return;
        }

        var allPrefixes = _options.ConventionalPrefixes.Values
                .SelectMany(arr => arr)
                .ToArray();

        if (_options.RemoveActionPrefix)
        {
            action.ActionName = action.ActionName
                .RemovePreFix(preFixes: allPrefixes)
                .ToKebabCase();
        }
        else
        {
            action.ActionName = action.ActionName.ToKebabCase();
        }
    }

    private void ConfigureComplexParameterBinding(ActionModel action)
    {
        var httpMethod = SelectHttpMethod(action);

        // 如果是GET请求，不自动处理复杂参数绑定
        if (httpMethod.Equals("Get", StringComparison.OrdinalIgnoreCase)) return;

        foreach (var parameter in action.Parameters)
        {
            if (parameter.BindingInfo != null) continue;

            if (TypeHelper.IsComplexType(parameter.ParameterType))
            {
                parameter.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
            }
        }
    }

    private void ConfigureActionRouteAttribute(ActionModel action)
    {

        foreach (var selector in action.Selectors)
        {
            string? template = string.Empty;// 路由模板为空
            string? actionRouteTemplate = selector.AttributeRouteModel?.Template;// Action路由模板
            string? controllerRouteTemplate = GetControllerRouteTemplate(action.Controller);// 控制器路由模板，为空时，则说明控制器不存在路由模版

            if (!string.IsNullOrEmpty(actionRouteTemplate))
            {
                //当控制器路由模版为空，Action存在路由模板时，生成路由模板并与控制器路由模板拼接
                if (string.IsNullOrEmpty(controllerRouteTemplate))
                {
                    controllerRouteTemplate = GenerateControllerRouteTemplate(action.Controller);
                    template = $"{controllerRouteTemplate}/{actionRouteTemplate}";
                }
                else
                {
                    template = actionRouteTemplate;
                }

                ValidateRouteTemplate(action, selector, template);
                selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(template));
                continue;
            }

            // 1. 处理空ActionName
            if (string.IsNullOrEmpty(action.ActionName) && action.Parameters.Count == 0 )
            {
                if (string.IsNullOrEmpty(controllerRouteTemplate))
                {
                    controllerRouteTemplate = GenerateControllerRouteTemplate(action.Controller);
                    template = controllerRouteTemplate;
                }

            }
            // 2. 常规动作路由生成
            else
            {
                // 检查控制器路由模版是否包含 [action] 令牌
                bool controllerRouteTemplateContainsActionToken = TemplateContainsActionToken(controllerRouteTemplate);

                //  ActionName 不为空且控制器路由不包含 [action] 时，添加 [action]
                string? actionRoute = (!string.IsNullOrEmpty(action.ActionName) && !controllerRouteTemplateContainsActionToken) ? "[action]" : null;

                if (string.IsNullOrEmpty(controllerRouteTemplate))
                {
                    controllerRouteTemplate = GenerateControllerRouteTemplate(action.Controller);
                    template = controllerRouteTemplate;
                }

                if (!string.IsNullOrEmpty(actionRoute))
                {
                    if (!string.IsNullOrEmpty(template))
                    {
                        template = $"{template}/{actionRoute}";
                    }
                    else
                    {
                        template = actionRoute;
                    }
                }

            }

            //为模板添加参数路由信息
            template = AddParametersToRoute(template, action);
            ValidateRouteTemplate(action, selector, template);

            if (!string.IsNullOrWhiteSpace(template))
            {
                selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(template));
            }
        }
    }

    private string GenerateControllerRouteTemplate(ControllerModel controller)
    {
        var segments = new List<string>();

        // 添加路由前缀
        if (_options.AddRoutePrefixToRoute && !string.IsNullOrWhiteSpace(_options.DefaultRoutePrefix))
        {
            segments.Add(_options.DefaultRoutePrefix);
        }

        // 添加根路径
        if (_options.AddRootPathToRoute && !string.IsNullOrWhiteSpace(_options.DefaultRootPath))
        {
            segments.Add(_options.DefaultRootPath);
        }

        // 添加控制器令牌
        segments.Add("[controller]");

        return (string.Join("/", segments));
    }

    private string SelectHttpMethod(ActionModel action)
    {
        var selector = action.Selectors.FirstOrDefault();
        if (selector != null)
        {
            var httpMethodConstraint = selector.ActionConstraints
                .FirstOrDefault(a => a is HttpMethodActionConstraint) as HttpMethodActionConstraint;

            if (httpMethodConstraint != null)
            {
                if (httpMethodConstraint.HttpMethods != null && httpMethodConstraint.HttpMethods.Any())
                {
                    return httpMethodConstraint.HttpMethods.First();
                }
            }
        }

        var httpMethodAttr = action.ActionMethod.GetCustomAttributes()
            .FirstOrDefault(a => a is HttpMethodAttribute) as HttpMethodAttribute;

        if (httpMethodAttr != null)
        {
            return httpMethodAttr.HttpMethods.First();
        }

        // 没有特性时使用前缀匹配
        foreach (var conventionalPrefix in _options.ConventionalPrefixes)
        {
            if (conventionalPrefix.Value.Any(prefix =>
                action.ActionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return conventionalPrefix.Key;
            }
        }

        return _options.DefaultHttpMethod;
    }

    private void ValidateRouteTemplate(ActionModel action, SelectorModel selector, string? template)
    {
        if (string.IsNullOrWhiteSpace(template)) return;

        string controllerName = action.Controller.ControllerType.Name;
        string actionName = action.ActionName ?? "未命名方法";

        if (Regex.IsMatch(template, @"\/{2,}"))
        {
            throw new InvalidOperationException(
                $"控制器 '{controllerName}' 中的方法 '{actionName}' 的路由模板 '{template}' " +
                "包含连续斜杠，这是不允许的。请检查路由配置。");
        }

        // 检查结尾斜杠（根路径除外）
        if (template.EndsWith('/') && template != "/")
        {
            throw new InvalidOperationException(
                $"控制器 '{controllerName}' 中的方法 '{actionName}' 的路由模板 '{template}' " +
                "以斜杠结尾，这是不允许的（根路径'/'除外）。请移除结尾斜杠。");
        }

        // 检查非根路径的开头斜杠
        if (template.StartsWith('/') && template.Length > 1)
        {
            throw new InvalidOperationException(
                $"控制器 '{controllerName}' 中的方法 '{actionName}' 的路由模板 '{template}' " +
                "以斜杠开头，这是不允许的（根路径'/'除外）。请移除开头斜杠。");
        }
    }

    private string? GetControllerRouteTemplate(ControllerModel controller)
    {
        var controllerRouteTemplate = controller.Selectors
            .FirstOrDefault()
            ?.AttributeRouteModel
            ?.Template;
        return controllerRouteTemplate;
    }

    private bool TemplateContainsActionToken(string? template)
    {
        // 检查模板是否包含 [action] 令牌（不区分大小写）
        return !string.IsNullOrEmpty(template) &&
               template.Contains("[action]", StringComparison.OrdinalIgnoreCase);
    }

    private string? AddParametersToRoute(string? template, ActionModel action)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            template = string.Empty;
        }

        if (!action.Parameters.Any())
        {
            return template;
        }

        var httpMethod = SelectHttpMethod(action).ToUpperInvariant();

        var pathParameters = GetSuitablePathParameters(action, httpMethod);

        if (!pathParameters.Any())
        {
            return template;
        }

        // 确保模板以斜杠结尾
        if (!string.IsNullOrEmpty(template) && !template.EndsWith('/'))
        {
            template += "/";
        }

        // 构建路径参数部分（按方法参数顺序）
        var parametersSegment = string.Join("/",pathParameters.Select(p => $"{{{p.Name.ToKebabCase()}}}"));

        return template + parametersSegment;
    }

    private IList<ParameterModel> GetSuitablePathParameters(ActionModel action, string httpMethod)
    {
        var allParameters = action.Parameters.ToList();
        var suitableParameters = new List<ParameterModel>();

        switch (httpMethod)
        {
            case "GET":
            case "DELETE":
            case "HEAD":
                // 对于GET/DELETE/HEAD：所有适合作为路由参数的类型
                suitableParameters.AddRange(allParameters.Where(p =>
                    TypeHelper.IsSuitableForRoute(p.ParameterType)));
                break;

            case "POST":
            case "PUT":
            case "PATCH":
                // 对于POST/PUT/PATCH：只添加名为"id"的适合参数
                suitableParameters.AddRange(allParameters.Where(p =>
                    p.Name.Equals("id", StringComparison.OrdinalIgnoreCase) &&
                    TypeHelper.IsSuitableForRoute(p.ParameterType)));
                break;
        }

        // 保持原始参数顺序
        return suitableParameters
            .OrderBy(p => allParameters.IndexOf(p))
            .ToList();
    }
}
