using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;
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
        RemoveEmptySelectors(action.Selectors);
        ConfigureActionSelectors(action);
        ConfigureActionHttpMethodAttribute(action);
        ConfigureActionName(action);
        ConfigureActionRouteAttribute(action);
    }

    private void ConfigureActionSelectors(ActionModel action)
    {
        if (!action.Selectors.Any())
        {
            action.Selectors.Add(new SelectorModel());
        }
    }

    private string? GenerateControllerRouteTemplate(ControllerModel controller)
    {
        if (controller.Selectors.Any(s => s.AttributeRouteModel != null))
        {
            return null;
        }

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

    private void ConfigureActionHttpMethodAttribute(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            if (selector.ActionConstraints.OfType<HttpMethodActionConstraint>().Any())
            {
                continue;
            }
            var httpMethod = SelectHttpMethod(action);
            selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { httpMethod }));
        }
    }

    private void ConfigureControllerRouteAttributes(ControllerModel controller)
    {
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

    internal string SelectHttpMethod(ActionModel action)
    {
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

    private void ConfigureActionRouteAttribute(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            if (selector.AttributeRouteModel != null)
            {
                ValidateRouteTemplate(action, selector, selector.AttributeRouteModel.Template);
                continue;
            }

            string? template;

            // 1. 处理空ActionName
            if (string.IsNullOrEmpty(action.ActionName))
            {
                template = GenerateControllerRouteTemplate(action.Controller);
            }
            // 2. 常规动作路由生成
            else
            {
                var controllerRoute = GenerateControllerRouteTemplate(action.Controller);

                // 检查控制器路由是否包含 [action] 令牌
                bool controllerRouteContainsActionToken = ControllerRouteContainsActionToken(action.Controller);

                string? actionRoute = controllerRouteContainsActionToken ? null : "[action]";

                if (string.IsNullOrEmpty(controllerRoute))
                {
                    template = actionRoute;
                }
                else if (string.IsNullOrEmpty(actionRoute))
                {
                    template = controllerRoute;
                }
                else
                {
                    template = $"{controllerRoute}/{actionRoute}";
                }
            }

            ValidateRouteTemplate(action, selector, template);

            if (!string.IsNullOrWhiteSpace(template))
            {
                selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(template));
            }
        }
    }

    private bool ControllerRouteContainsActionToken(ControllerModel controller)
    {
        // 获取控制器第一个选择器的路由模板
        var controllerRouteTemplate = controller.Selectors
            .FirstOrDefault()
            ?.AttributeRouteModel
            ?.Template;

        // 检查模板是否包含 [action] 令牌（不区分大小写）
        return !string.IsNullOrEmpty(controllerRouteTemplate) &&
               controllerRouteTemplate.Contains("[action]", StringComparison.OrdinalIgnoreCase);
    }
}
