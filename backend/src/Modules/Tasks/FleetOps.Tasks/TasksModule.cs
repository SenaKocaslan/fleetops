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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/tasks/ping", () => Results.Ok(new { module = "Tasks" }));
    }
}
