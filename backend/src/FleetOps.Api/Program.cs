using FleetOps.Fleet;
using FleetOps.SharedKernel;
using FleetOps.Stock;
using FleetOps.Tasks;

var builder = WebApplication.CreateBuilder(args);

const string AngularPolitikasi = "angular";

var izinliOriginler = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy(AngularPolitikasi, politika => politika
        .WithOrigins(izinliOriginler)
        .AllowAnyHeader()
        .AllowAnyMethod()
        // SignalR istemcisi negotiate isteginde kimlik bilgisi gonderir.
        // AllowAnyOrigin ile birlikte kullanilamaz; origin listesi acik oldugu
        // icin sorun degil.
        .AllowCredentials()));

builder.Services
    .AddModule<FleetModule>(builder.Configuration)
    .AddModule<TasksModule>(builder.Configuration)
    .AddModule<StockModule>(builder.Configuration);

var app = builder.Build();

app.UseCors(AngularPolitikasi);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapModuleEndpoints();

app.Run();

// WebApplicationFactory'nin erisebilmesi icin; kaldirilirsa integration testler derlenmez.
public partial class Program;
