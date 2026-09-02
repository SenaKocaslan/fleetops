using FleetOps.Fleet;
using FleetOps.SharedKernel;
using FleetOps.Stock;
using FleetOps.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddModule<FleetModule>(builder.Configuration)
    .AddModule<TasksModule>(builder.Configuration)
    .AddModule<StockModule>(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapModuleEndpoints();

app.Run();
public partial class Program;
