using System.Net.Http.Headers;
using System.Net.Http.Json;
using FleetOps.Api;
using FleetOps.Fleet.Persistence;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using FleetOps.Api.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

        // Sarj yonlendirici testin ortasinda arac durumu degistirirse
        // testler flaky olur; tur testten dogrudan cagriliyor.
        builder.UseSetting("Sarj:Interval", "01:00:00");

        // Simulator surekli telemetri yazarsa AGV durumu testin altindan kayar.
        builder.UseSetting("Simulator:Enabled", "false");

        // Olu mektup testinin bes tur donmesi gereksiz; sinir dusuruldu.
        // Testler degeri buradan degil IOptions'tan okuyor.
        builder.UseSetting("Outbox:MaxAttempts", "3");

        builder.UseSetting("Jwt:SigningKey", "test-imza-anahtari-en-az-32-bayt-uzunlugunda-olmali");
    }

    public async Task InitializeAsync()
    {
        await _veritabani.StartAsync();

        await MigrationUygulaAsync();
    }

    // Uretimdeki "--migrate" adimiyla AYNI kodu cagirir. Ayri bir liste
    // tutulsaydi, bir modul uretimde goc almadigi halde testler yesil kalirdi.
    public Task MigrationUygulaAsync() =>
        VeritabaniGocleri.UygulaAsync(Services);

    // Tohum AGV sayisi uc ve testler ayni araclari paylasiyor. "Bir AGV'nin
    // en fazla bir acik atamasi olur" kurali geldikten sonra onceki testten
    // kalan acik atama sonrakini engelliyor; her test kendi baslangicini
    // temizliyor. Kural degil, test yalitimi sorunu.
    public async Task FiloyuHazirlaAsync()
    {
        using var kapsam = KapsamAc();

        var tasks = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await tasks.Database.ExecuteSqlRawAsync(
            "UPDATE tasks.task_assignment SET completed_at_utc = now() "
            + "WHERE completed_at_utc IS NULL");

        // Atama ve kilit artik aracin Fleet'teki durumuna bakiyor. Sarj
        // testleri araclari servis disi birakabildigi icin, araca ihtiyaci
        // olan her test filoyu kendi bilinen durumuna getiriyor.
        var fleet = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        foreach (var agv in await fleet.Agvs.ToListAsync())
        {
            agv.ServiseAl();
            agv.BataryaBildir(100);
        }

        await fleet.SaveChangesAsync();
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
