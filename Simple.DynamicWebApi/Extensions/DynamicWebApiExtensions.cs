using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Simple.DynamicWebApi.Conventions;
using Simple.DynamicWebApi.Options;
using Simple.DynamicWebApi.Providers;

namespace Simple.DynamicWebApi.Extensions;

public static class DynamicWebApiExtensions
{

    /// <summary>
    /// 为IServiceCollection添加动态API控制器扩展方法。
    /// </summary>
    /// <param name="services">IServiceCollection类型的服务集合。</param>
    /// <param name="configureOptions">用于配置DynamicWebApiOptions的操作，可空。</param>
    /// <returns>IServiceCollection类型的服务集合。</returns>
    public static IServiceCollection AddDynamicApiController(this IServiceCollection services, Action<DynamicWebApiSettingsOptions> configureOptions)
    {
        var dynamicWebApiOptions = new DynamicWebApiSettingsOptions();
        configureOptions.Invoke(dynamicWebApiOptions);
        return services.AddDynamicApiController(dynamicWebApiOptions);
    }


    /// <summary>
    /// IServiceCollection添加动态API控制器扩展方法。
    /// </summary>
    /// <param name="services">IServiceCollection类型的服务集合。</param>
    /// <param name="configureOptions">动态WebApi配置选项</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">IServiceCollection类型的服务集合。</exception>
    public static IServiceCollection AddDynamicApiController(this IServiceCollection services, DynamicWebApiSettingsOptions? dynamicWebApiOptions = default)
    {
        dynamicWebApiOptions ??= new DynamicWebApiSettingsOptions();

        if (dynamicWebApiOptions.EnableDynamicWebApi == false)
        {
            //不启用动态API控制器
            return services;
        }

        //AddDynamicApiController 必须在 AddControllers 之后调用
        var partManager = services.FirstOrDefault(s => s.ServiceType == typeof(ApplicationPartManager))?.ImplementationInstance as ApplicationPartManager;
        if (partManager is null)
        {
            throw new InvalidOperationException(
                $" {nameof(AddDynamicApiController)} 必须在在 {nameof(MvcServiceCollectionExtensions.AddControllers)} 之后进行调用"
            );
        }

        //动态API控制器特性提供者,用于动态发现和注册 API 控制器
        partManager.FeatureProviders.Add(new DynamicWebApiFeatureProvider());

        IServiceCollection serviceCollection = services.Configure<MvcOptions>(o =>
        {
            //动态API控制器路由约定
            o.Conventions.Add(new DynamicWebApiConvention(dynamicWebApiOptions));
        });

        return services;
    }
}
