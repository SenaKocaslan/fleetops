using FleetOps.Fleet;
using FleetOps.SharedKernel;
using FleetOps.Stock;
using FleetOps.Tasks;

var builder = WebApplication.CreateBuilder(args);

const string AngularPolitikasi = "angular";

// Izinli origin'ler koda gomulmez: uretimde farkli olacak.
var izinliOriginler = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy(AngularPolitikasi, politika => politika
        .WithOrigins(izinliOriginler)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Composition root: moduller kendilerini kaydeder, Program.cs iclerini bilmez.
builder.Services
    .AddModule<FleetModule>(builder.Configuration)
    .AddModule<TasksModule>(builder.Configuration)
    .AddModule<StockModule>(builder.Configuration);

var app = builder.Build();

app.UseCors(AngularPolitikasi);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapModuleEndpoints();

app.Run();

// Integration testlerin WebApplicationFactory ile erisebilmesi icin.
public partial class Program;
