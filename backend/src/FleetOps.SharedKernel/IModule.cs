using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.SharedKernel;

/// <summary>
/// Bir modulun kendini sisteme tanitma sozlesmesi. Program.cs modullerin
/// icini bilmez; her modul kendi servislerini ve uc noktalarini kendi kaydeder.
/// </summary>
public interface IModule
{
    string Name { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public static class ModuleExtensions
{
    /// <summary>
    /// Modulu kaydeder. Modul ornegi DI'a da eklenir; boylece uc nokta
    /// eslemesi static liste tutmadan yapilabilir. Static liste, testlerde
    /// uygulama birden cok kez ayaga kalkinca birikir ve sizinti yaratirdi.
    /// </summary>
    public static IServiceCollection AddModule<TModule>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TModule : class, IModule, new()
    {
        var module = new TModule();
        module.RegisterServices(services, configuration);
        services.AddSingleton<IModule>(module);
        return services;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in endpoints.ServiceProvider.GetServices<IModule>())
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
