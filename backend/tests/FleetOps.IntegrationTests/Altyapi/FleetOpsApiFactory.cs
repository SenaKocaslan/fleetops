using FleetOps.Fleet.Persistence;
using FleetOps.Tasks.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FleetOps.IntegrationTests.Altyapi;

// Gercek PostgreSQL uzerinde calisan test uygulamasi.
// In-memory provider kullanilmiyor cunku bu projede test edilmesi gereken
// seyler (optimistic concurrency, jsonb, snake_case eslemesi, gercek SQL)
// in-memory'de taklit edilmez; orada gecen test uretimde patlar.
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
        // Uygulama kodu degismiyor; yalnizca yapilandirma ezilir.
        builder.UseSetting("ConnectionStrings:FleetOps", _veritabani.GetConnectionString());

        // Arka plandaki LockReaper testin ortasinda calisip kilitleri
        // birakirsa testler flaky olur. Zamanlayiciyi pratikte devre disi
        // birakiyoruz; reaper'in kendisi BirTurCalistirAsync ile dogrudan
        // cagirilarak test ediliyor.
        builder.UseSetting("ResourceLock:ReaperInterval", "01:00:00");
    }

    public async Task InitializeAsync()
    {
        await _veritabani.StartAsync();

        // Migration'lar ACILISTA degil, ayri bir adim olarak uygulanir.
        // Uygulama Program.cs'te migrate etmez - cok instance'li dagitimda
        // yaris kosulu olusurdu.
        await MigrationUygulaAsync();
    }

    public async Task MigrationUygulaAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<FleetDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TasksDbContext>().Database.MigrateAsync();
    }

    // Test icinde dogrudan veritabani islemi icin kapsam acar.
    public IServiceScope KapsamAc() => Services.CreateScope();

    // xUnit v2'nin IAsyncLifetime.DisposeAsync'i Task dondurur;
    // WebApplicationFactory'nin DisposeAsync'i ise ValueTask. Ikisi ayni
    // imzayla karsilanamaz, bu yuzden acik arayuz uygulamasi kullanilir.
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
