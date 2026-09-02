using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.Fleet;

public sealed class FleetModule : IModule
{
    public string Name => "Fleet";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FleetDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("FleetOps"),
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    FleetDbContext.Schema))
            // PostgreSQL'de tirnaksiz tanimlayici kucuk harfe duser; snake_case
            // hem yerlesik gelenek hem de elle SQL yazmayi kolaylastirir.
            .UseSnakeCaseNamingConvention());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        
        endpoints.MapGet("/api/fleet/ping", () => Results.Ok(new { module = "Fleet" }));
    }
}
