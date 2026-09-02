using FleetOps.Fleet;
using FleetOps.SharedKernel;
using FleetOps.Stock;
using FleetOps.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Composition root: moduller kendilerini kaydeder, Program.cs iclerini bilmez.
builder.Services
    .AddModule<FleetModule>(builder.Configuration)
    .AddModule<TasksModule>(builder.Configuration)
    .AddModule<StockModule>(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapModuleEndpoints();

app.Run();

/// <summary>Integration testlerin WebApplicationFactory ile erisebilmesi icin.</summary>
public partial class Program;
