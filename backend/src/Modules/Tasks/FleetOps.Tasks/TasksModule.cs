using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using FleetOps.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.Tasks;


public sealed class TasksModule : IModule
{
    public string Name => "Tasks";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TasksDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("FleetOps"),
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    TasksDbContext.Schema))
            // PostgreSQL'de tirnaksiz tanimlayici kucuk harfe duser; snake_case
            // hem yerlesik gelenek hem de elle SQL yazmayi kolaylastirir.
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IQueryHandler<ListTasksQuery, IReadOnlyList<TaskSummary>>, ListTasksQueryHandler>();
        services.AddScoped<ICommandHandler<CreateTaskCommand, Guid>, CreateTaskCommandHandler>();
        services.AddScoped<ICommandHandler<AssignTaskCommand>, AssignTaskCommandHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var grup = endpoints.MapGroup("/api/tasks").WithTags("Tasks");

        grup.MapGet("/", async (
            IQueryHandler<ListTasksQuery, IReadOnlyList<TaskSummary>> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new ListTasksQuery(), ct);
            return Results.Ok(sonuc.Value);
        });

        grup.MapPost("/", async (
            CreateTaskCommand command,
            ICommandHandler<CreateTaskCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(command, ct);

            // Beklenen is hatasi 400 doner; exception'a cevrilmez.
            return sonuc.IsSuccess
                ? Results.Created($"/api/tasks/{sonuc.Value}", new { id = sonuc.Value })
                : HataYaniti(sonuc.Error);
        });

        grup.MapPost("/{id:guid}/assign", async (
            Guid id,
            AssignTaskRequest istek,
            ICommandHandler<AssignTaskCommand> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new AssignTaskCommand(id, istek.AgvId), ct);

            return sonuc.IsSuccess ? Results.NoContent() : HataYaniti(sonuc.Error);
        });
    }

    // Hata -> HTTP durum kodu esleme tek yerde. Kodu metin olarak
    // karsilastirmak yerine Error kaydinin kendisiyle karsilastiriyorum:
    // kod adi degisirse derleyici burayi da bulur.
    private static IResult HataYaniti(Error hata)
    {
        var govde = new { code = hata.Code, message = hata.Message };

        if (hata == TaskErrors.Bulunamadi)
        {
            return Results.NotFound(govde);
        }

        // 409: istemci yanlis bir sey gondermedi, yarisi kaybetti.
        // Ayni istegi tekrar gondermesi anlamli - 400'de degildir.
        if (hata == TaskErrors.EszamanliDegisiklik)
        {
            return Results.Conflict(govde);
        }

        return Results.BadRequest(govde);
    }
}

// Gorev kimligi URL'den geliyor; govdede yalnizca AGV var.
public sealed record AssignTaskRequest(Guid AgvId);
