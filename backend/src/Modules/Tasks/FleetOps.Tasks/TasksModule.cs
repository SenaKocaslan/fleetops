using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;
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
                : Results.BadRequest(new { code = sonuc.Error.Code, message = sonuc.Error.Message });
        });
    }
}
