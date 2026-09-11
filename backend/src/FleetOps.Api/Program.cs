using FleetOps.Api;
using FleetOps.Api.Auth;
using FleetOps.Api.Dispatch;
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

builder.Services.AddFleetOpsAuth(builder.Configuration);
builder.Services.AddDispatch();
builder.Services.AddFleetOpsOpenApi();

// Zaman asimi Docker HEALTHCHECK'in 3 saniyesiyle ayni: ulasilamayan bir
// sunucuda baglanti denemesi varsayilan olarak 15 saniye bekler.
builder.Services.AddHealthChecks()
    .AddCheck<VeritabaniSaglikKontrolu>("veritabani", timeout: TimeSpan.FromSeconds(3));

builder.Services
    .AddModule<FleetModule>(builder.Configuration)
    .AddModule<TasksModule>(builder.Configuration)
    .AddModule<StockModule>(builder.Configuration);

var app = builder.Build();

// Goc ayri bir adim; normal acilista calismaz. Bkz. VeritabaniGocleri.
if (args.Contains(VeritabaniGocleri.Arguman))
{
    await VeritabaniGocleri.UygulaAsync(app.Services);
    return;
}

app.UseCors(AngularPolitikasi);

// Sira onemli: once kimin oldugu (authentication), sonra ne yapabildigi
// (authorization). Ters cevrilirse yetkilendirme her zaman anonim gorur.
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// Yalnizca gelistirmede. API haritasini uretimde herkese acmak gereksiz bir
// bilgi sizintisi; ihtiyac duyan gelistirici yerelde calistirip alir.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapAuthEndpoints();
app.MapAlarmEndpoints();
app.MapDispatchEndpoints();
app.MapModuleEndpoints();

app.Run();

// WebApplicationFactory'nin erisebilmesi icin; kaldirilirsa integration testler derlenmez.
public partial class Program;
