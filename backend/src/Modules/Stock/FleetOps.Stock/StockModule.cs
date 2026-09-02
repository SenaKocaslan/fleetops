using FleetOps.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.Stock;

public sealed class StockModule : IModule
{
    public string Name => "Stock";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
       
        endpoints.MapGet("/api/stock/ping", () => Results.Ok(new { module = "Stock" }));
    }
}
