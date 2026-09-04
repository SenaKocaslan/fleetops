using System.Net;
using System.Net.Http.Json;
using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Infrastructure;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class TelemetriTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Agv03 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Lokasyon = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    [Fact]
    public async Task Telemetri_bataryayi_konumu_ve_son_gorulmeyi_yazar()
    {
        var istemci = fabrika.CreateClient();
        var once = DateTime.UtcNow;

        var yanit = await istemci.PostAsJsonAsync(
            $"/api/agvs/{Agv01}/telemetry",
            new { batteryLevel = 77, locationId = Lokasyon });

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);

        var agv = await AgvOkuAsync(Agv01);
        Assert.Equal(77, agv.BatteryLevel);
        Assert.Equal(Lokasyon, agv.CurrentLocationId);
        Assert.NotNull(agv.LastSeenAtUtc);
        Assert.True(agv.LastSeenAtUtc >= once);
    }

    [Fact]
    public async Task Bilinmeyen_agv_icin_404_doner()
    {
        var yanit = await fabrika.CreateClient().PostAsJsonAsync(
            $"/api/agvs/{Guid.NewGuid()}/telemetry", new { batteryLevel = 50 });

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Gecersiz_batarya_400_ve_hata_kodu_doner(int batarya)
    {
        var yanit = await fabrika.CreateClient().PostAsJsonAsync(
            $"/api/agvs/{Agv01}/telemetry", new { batteryLevel = batarya });

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);

        var govde = await yanit.Content.ReadFromJsonAsync<HataGovdesi>();
        Assert.Equal(FleetErrors.BataryaAraligiDisi.Code, govde!.Code);
    }

    [Fact]
    public async Task Telemetri_signalr_uzerinden_yayinlanir()
    {
        await using var baglanti = HubBaglantisiKur();
        var gelenler = new List<AgvSummary>();
        var ilkYayin = new TaskCompletionSource<AgvSummary>();

        baglanti.On<AgvSummary>(FleetHub.AgvDegisti, agv =>
        {
            gelenler.Add(agv);
            ilkYayin.TrySetResult(agv);
        });

        await baglanti.StartAsync();

        await fabrika.CreateClient().PostAsJsonAsync(
            $"/api/agvs/{Agv01}/telemetry", new { batteryLevel = 64, locationId = Lokasyon });

        var yayin = await ilkYayin.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(Agv01, yayin.Id);
        Assert.Equal("AGV-01", yayin.Code);
        Assert.Equal(64, yayin.BatteryLevel);
        Assert.Equal(Lokasyon, yayin.CurrentLocationId);
    }

    [Fact]
    public async Task Basarisiz_telemetri_yayinlanmaz()
    {
        await using var baglanti = HubBaglantisiKur();
        var yayinSayisi = 0;
        baglanti.On<AgvSummary>(FleetHub.AgvDegisti, _ => Interlocked.Increment(ref yayinSayisi));
        await baglanti.StartAsync();

        await fabrika.CreateClient().PostAsJsonAsync(
            $"/api/agvs/{Agv01}/telemetry", new { batteryLevel = 500 });

        // Yayin gelmedigini beklemek icin kisa bir pencere; gelseydi burada gorulurdu.
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal(0, yayinSayisi);
    }

    [Fact]
    public async Task Simulator_bir_turda_tum_araclara_telemetri_gonderir()
    {
        var simulator = fabrika.Services.GetServices<IHostedService>().OfType<AgvSimulator>().Single();

        var araeSayisi = await AracSayisiAsync();
        var gonderilen = await simulator.BirTurCalistirAsync(CancellationToken.None);

        Assert.Equal(araeSayisi, gonderilen);
    }

    [Fact]
    public async Task Simulator_sarjdaki_aracin_bataryasini_doldurur()
    {
        var simulator = fabrika.Services.GetServices<IHostedService>().OfType<AgvSimulator>().Single();

        await BataryaAyarlaAsync(Agv03, 40);
        var once = (await AgvOkuAsync(Agv03)).BatteryLevel;

        await simulator.BirTurCalistirAsync(CancellationToken.None);

        var sonra = (await AgvOkuAsync(Agv03)).BatteryLevel;
        Assert.True(sonra > once, $"Sarjdaki arac dolmali: {once} -> {sonra}");
    }

    private HubConnection HubBaglantisiKur() => new HubConnectionBuilder()
        .WithUrl(new Uri(fabrika.Server.BaseAddress, FleetHub.Yol), secenekler =>
        {
            // TestServer'in WebSocket'i yok; istek bu handler uzerinden
            // dogrudan uygulamaya gider.
            secenekler.HttpMessageHandlerFactory = _ => fabrika.Server.CreateHandler();
        })
        .Build();

    private async Task<Agv> AgvOkuAsync(Guid id)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        return await db.Agvs.AsNoTracking().SingleAsync(a => a.Id == id);
    }

    private async Task<int> AracSayisiAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        return await db.Agvs.CountAsync();
    }

    private async Task BataryaAyarlaAsync(Guid id, int batarya)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var agv = await db.Agvs.SingleAsync(a => a.Id == id);
        agv.BataryaBildir(batarya);
        await db.SaveChangesAsync();
    }

    private sealed record HataGovdesi(string Code, string Message);
}
