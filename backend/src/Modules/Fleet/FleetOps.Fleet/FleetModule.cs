using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Infrastructure;
using FleetOps.Fleet.Integration;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
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
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IQueryHandler<ListAgvsQuery, IReadOnlyList<AgvSummary>>, ListAgvsQueryHandler>();
        services.AddScoped<ICommandHandler<ReportTelemetryCommand>, ReportTelemetryCommandHandler>();

        services.AddSignalR();
        services.AddSingleton<IFleetNotifier, SignalRFleetNotifier>();

        services.Configure<SimulatorOptions>(configuration.GetSection(SimulatorOptions.Bolum));
        services.AddHostedService<AgvSimulator>();

        services.AddScoped<IIntegrationEventHandler, GorevAtandigindaAgvMesgullestir>();
        services.AddScoped<IIntegrationEventHandler, GorevTamamlandigindaAgvSerbestBirak>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<FleetHub>(FleetHub.Yol);

        var grup = endpoints.MapGroup("/api/agvs").WithTags("Fleet");

        grup.MapGet("/", async (
            IQueryHandler<ListAgvsQuery, IReadOnlyList<AgvSummary>> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new ListAgvsQuery(), ct);
            return Results.Ok(sonuc.Value);
        });

        grup.MapPost("/{id:guid}/telemetry", async (
            Guid id,
            TelemetriIstegi istek,
            ICommandHandler<ReportTelemetryCommand> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(
                new ReportTelemetryCommand(id, istek.BatteryLevel, istek.LocationId), ct);

            return sonuc.IsSuccess ? Results.NoContent() : HataYaniti(sonuc.Error);
        });
    }

    private static IResult HataYaniti(Error hata)
    {
        var govde = new { code = hata.Code, message = hata.Message };

        if (hata == FleetErrors.Bulunamadi)
        {
            return Results.NotFound(govde);
        }

        // Istemci yanlis bir sey gondermedi, yarisi kaybetti.
        if (hata == FleetErrors.EszamanliDegisiklik)
        {
            return Results.Conflict(govde);
        }

        return Results.BadRequest(govde);
    }
}

public sealed record TelemetriIstegi(int BatteryLevel, Guid? LocationId);
