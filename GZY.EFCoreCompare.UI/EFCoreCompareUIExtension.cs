using GZY.EFCoreCompare.Core;
using GZY.EFCoreCompare.UI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class EFCoreCompareUIExtension
{
    public static List<Type>? DBContextTypes =null;
    /// <summary>
    /// 添加EFCoreCompareUI服务,需放在AddDbContext之后
    /// </summary>
    /// <typeparam name="T">DbContext类型</typeparam>
    /// <param name="services">IServiceCollection实例</param>
    /// <param name="config">CompareEFCore配置</param>
    /// <returns>更新后的IServiceCollection实例</returns>
    public static IServiceCollection AddEFCoreCompareUI(this IServiceCollection services, CompareEFCoreConfig? config = null)
    {
        // 添加Razor Pages服务
        services.AddRazorPages();
        // 添加HttpClient服务
        services.AddHttpClient();
        // 添加HttpContextAccessor服务
        services.AddHttpContextAccessor();
        DBContextTypes= services.GetAllRegisteredDbContextTypes();
        // 添加CompareEFCore
        services.AddScoped<CompareEFCore>(sp =>
        {
            return new CompareEFCore(config);
        });

        return services;
    }

    /// <summary>
    /// 使用EFCoreCompareUI中间件
    /// </summary>
    /// <param name="builder">IApplicationBuilder实例</param>
    /// <returns>更新后的IApplicationBuilder实例</returns>
    public static IApplicationBuilder UseEFCoreCompareUI(this IApplicationBuilder builder)
    {
        // 使用基本认证中间件
        builder.UseMiddleware<EFCoreCompareUIBasicAuthMiddleware>();
        // 使用路由
        builder.UseRouting();
        // 使用静态文件
        builder.UseStaticFiles();
        // 配置终结点
        builder.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
        });
        IServiceProvider services = builder.ApplicationServices;
        return builder;
    }
}
