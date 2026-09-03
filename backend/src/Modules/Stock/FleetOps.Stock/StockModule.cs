using FleetOps.SharedKernel;
using FleetOps.Stock.Application;
using FleetOps.Stock.Integration;
using FleetOps.Stock.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.Stock;

public sealed class StockModule : IModule
{
    public string Name => "Stock";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StockDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("FleetOps"),
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    StockDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IQueryHandler<ListLocationsQuery, IReadOnlyList<LocationSummary>>, ListLocationsQueryHandler>();
        services.AddScoped<IQueryHandler<ListStockMovementsQuery, IReadOnlyList<StockMovementSummary>>, ListStockMovementsQueryHandler>();

        // Stock, Tasks'in olaylarini dinler. Tasks Stock'u cagirmaz.
        services.AddScoped<IIntegrationEventHandler, GorevTamamlandigindaStokHareketiOlustur>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/locations", async (
            IQueryHandler<ListLocationsQuery, IReadOnlyList<LocationSummary>> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new ListLocationsQuery(), ct);
            return Results.Ok(sonuc.Value);
        }).WithTags("Stock");

        endpoints.MapGet("/api/stock/movements", async (
            IQueryHandler<ListStockMovementsQuery, IReadOnlyList<StockMovementSummary>> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new ListStockMovementsQuery(), ct);
            return Results.Ok(sonuc.Value);
        }).WithTags("Stock");
    }
}
