using System.Net.Http.Headers;
using System.Net.Http.Json;
using FleetOps.Api.Auth;
using FleetOps.Fleet.Persistence;
using FleetOps.Stock.Persistence;
using FleetOps.Tasks.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FleetOps.IntegrationTests.Altyapi;

public sealed class FleetOpsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _veritabani = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fleetops")
        .WithUsername("fleetops")
        .WithPassword("fleetops")
        .Build();

    public string BaglantiMetni => _veritabani.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:FleetOps", _veritabani.GetConnectionString());

        // Arka plan servisleri testin ortasinda calisirsa testler flaky olur.
        // Zamanlayicilar kapali; servisler testten dogrudan cagriliyor.
        builder.UseSetting("ResourceLock:ReaperInterval", "01:00:00");

        builder.UseSetting("Outbox:PollInterval", "01:00:00");

        // Simulator surekli telemetri yazarsa AGV durumu testin altindan kayar.
        builder.UseSetting("Simulator:Enabled", "false");

        builder.UseSetting("Jwt:SigningKey", "test-imza-anahtari-en-az-32-bayt-uzunlugunda-olmali");
    }

    public async Task InitializeAsync()
    {
        await _veritabani.StartAsync();

        await MigrationUygulaAsync();
    }

    public async Task MigrationUygulaAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<FleetDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TasksDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<StockDbContext>().Database.MigrateAsync();
    }

    // Tohum kullanicilar migration'da; testler gercek login akisindan geciyor,
    // token elle imzalanmiyor. Boylece login bozulursa testler de kirilir.
    public const string OperatorAdi = "operator";
    public const string OperatorParolasi = "Operator123!";
    public const string SupervisorAdi = "supervisor";
    public const string SupervisorParolasi = "Supervisor123!";

    public async Task<HttpClient> IstemciAsync(
        string kullaniciAdi = SupervisorAdi,
        string parola = SupervisorParolasi)
    {
        var istemci = CreateClient();
        istemci.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await TokenAsync(kullaniciAdi, parola));

        return istemci;
    }

    public async Task<string> TokenAsync(
        string kullaniciAdi = SupervisorAdi,
        string parola = SupervisorParolasi)
    {
        var yanit = await CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { userName = kullaniciAdi, password = parola });

        yanit.EnsureSuccessStatusCode();

        var govde = await yanit.Content.ReadFromJsonAsync<LoginYaniti>();
        return govde!.Token;
    }

    public IServiceScope KapsamAc() => Services.CreateScope();

    // xUnit v2 Task, WebApplicationFactory ValueTask donduruyor; acik
    // arayuz uygulamasi zorunlu.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _veritabani.DisposeAsync();
    }
}

[CollectionDefinition(Ad)]
public sealed class VeritabaniKoleksiyonu : ICollectionFixture<FleetOpsApiFactory>
{
    public const string Ad = "Veritabani";
}
