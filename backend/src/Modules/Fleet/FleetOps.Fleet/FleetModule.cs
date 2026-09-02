using FleetOps.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.Fleet;

/// <summary>
/// Fleet modulunun sisteme kayit noktasi. Program.cs bu modulun icini bilmez.
/// </summary>
public sealed class FleetModule : IModule
{
    public string Name => "Fleet";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Gun 2'den itibaren: DbContext, handler'lar, repository'ler.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Iskelet dogrulamasi icin gecici uc nokta.
        endpoints.MapGet("/api/fleet/ping", () => Results.Ok(new { module = "Fleet" }));
    }
}
