using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Infrastructure;
using FleetOps.Tasks.Persistence;
using FleetOps.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IQueryHandler<ListTasksQuery, PagedResult<TaskSummary>>, ListTasksQueryHandler>();
        services.AddScoped<ICommandHandler<CreateTaskCommand, Guid>, CreateTaskCommandHandler>();
        services.AddScoped<ICommandHandler<AssignTaskCommand>, AssignTaskCommandHandler>();
        services.AddScoped<ICommandHandler<StartTaskCommand>, StartTaskCommandHandler>();
        services.AddScoped<ICommandHandler<CompleteTaskCommand>, CompleteTaskCommandHandler>();

        services.Configure<ResourceLockOptions>(
            configuration.GetSection(ResourceLockOptions.Bolum));

        services.AddScoped<IQueryHandler<ListResourcesQuery, IReadOnlyList<ResourceSummary>>, ListResourcesQueryHandler>();
        services.AddScoped<ICommandHandler<AcquireLockCommand, Guid>, AcquireLockCommandHandler>();
        services.AddScoped<ICommandHandler<ReleaseLockCommand>, ReleaseLockCommandHandler>();
        services.AddScoped<ICommandHandler<ReapExpiredLocksCommand, int>, ReapExpiredLocksCommandHandler>();

        services.Configure<TasksAlarmOptions>(configuration.GetSection(TasksAlarmOptions.Bolum));
        services.AddScoped<IAlarmSource, GorevAlarmKaynagi>();

        services.AddHostedService<LockReaper>();

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.Bolum));
        services.AddSingleton<IIntegrationEventTypeRegistry, IntegrationEventTypeRegistry>();
        services.AddHostedService<OutboxDispatcher>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Grup seviyesinde varsayilan: giris yapmis herkes okuyabilir.
        // Yazma uc noktalari kendi politikasiyla bunu daraltiyor.
        var grup = endpoints.MapGroup("/api/tasks").WithTags("Tasks")
            .RequireAuthorization(Politikalar.Okuma);

        grup.MapGet("/", async (
            int? page,
            int? pageSize,
            string? materialCode,
            IQueryHandler<ListTasksQuery, PagedResult<TaskSummary>> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(
                new ListTasksQuery(new PageRequest(page, pageSize), materialCode), ct);

            return Results.Ok(sonuc.Value);
        });

        grup.MapPost("/", async (
            CreateTaskCommand command,
            ICommandHandler<CreateTaskCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(command, ct);

            return sonuc.IsSuccess
                ? Results.Created($"/api/tasks/{sonuc.Value}", new { id = sonuc.Value })
                : HataYaniti(sonuc.Error);
        }).RequireAuthorization(Politikalar.GorevPlanlama);

        grup.MapPost("/{id:guid}/assign", async (
            Guid id,
            AssignTaskRequest istek,
            ICommandHandler<AssignTaskCommand> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new AssignTaskCommand(id, istek.AgvId), ct);

            return sonuc.IsSuccess ? Results.NoContent() : HataYaniti(sonuc.Error);
        }).RequireAuthorization(Politikalar.GorevPlanlama);

        grup.MapPost("/{id:guid}/start", async (
            Guid id,
            ICommandHandler<StartTaskCommand> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new StartTaskCommand(id), ct);
            return sonuc.IsSuccess ? Results.NoContent() : HataYaniti(sonuc.Error);
        }).RequireAuthorization(Politikalar.GorevYurutme);

        grup.MapPost("/{id:guid}/complete", async (
            Guid id,
            ICommandHandler<CompleteTaskCommand> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new CompleteTaskCommand(id), ct);
            return sonuc.IsSuccess ? Results.NoContent() : HataYaniti(sonuc.Error);
        }).RequireAuthorization(Politikalar.GorevYurutme);

        var kaynaklar = endpoints.MapGroup("/api/resources").WithTags("Resources")
            .RequireAuthorization(Politikalar.Okuma);

        kaynaklar.MapGet("/", async (
            IQueryHandler<ListResourcesQuery, IReadOnlyList<ResourceSummary>> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new ListResourcesQuery(), ct);
            return Results.Ok(sonuc.Value);
        });

        kaynaklar.MapPost("/{id:guid}/lock", async (
            Guid id,
            AgvIstegi istek,
            ICommandHandler<AcquireLockCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new AcquireLockCommand(id, istek.AgvId), ct);

            return sonuc.IsSuccess
                ? Results.Ok(new { lockId = sonuc.Value })
                : HataYaniti(sonuc.Error);
        }).RequireAuthorization(Politikalar.GorevYurutme);

        kaynaklar.MapPost("/{id:guid}/release", async (
            Guid id,
            AgvIstegi istek,
            ICommandHandler<ReleaseLockCommand> handler,
            CancellationToken ct) =>
        {
            var sonuc = await handler.HandleAsync(new ReleaseLockCommand(id, istek.AgvId), ct);

            return sonuc.IsSuccess ? Results.NoContent() : HataYaniti(sonuc.Error);
        }).RequireAuthorization(Politikalar.GorevYurutme);
    }

    private static IResult HataYaniti(Error hata)
    {
        var govde = new { code = hata.Code, message = hata.Message };

        if (hata == TaskErrors.Bulunamadi
            || hata == ResourceErrors.Bulunamadi
            || hata == ResourceErrors.KilitBulunamadi)
        {
            return Results.NotFound(govde);
        }

        if (hata == TaskErrors.EszamanliDegisiklik
            || hata == ResourceErrors.KaynakMesgul
            || hata == ResourceErrors.KilidiBaskasiTutuyor)
        {
            return Results.Conflict(govde);
        }

        return Results.BadRequest(govde);
    }
}

public sealed record AssignTaskRequest(Guid AgvId);

public sealed record AgvIstegi(Guid AgvId);
