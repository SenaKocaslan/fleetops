using System.Net.Http.Json;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;
using FleetOps.Fleet.Application;
using FleetOps.Fleet.Integration;
using FleetOps.Tasks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class IntegrationEventEszamanlilikTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Gun 7'de eklenen telemetri AGV satirini surekli guncelliyor. Gun 8'de
    // olculdu: outbox_message.error = "expected to affect 1 row(s), but
    // actually affected 0 row(s)" -- integration event handler'i yarisi
    // kaybedince atama olayi hic teslim edilemiyordu.
    // Yarisi TETIKLEYEN test: catisma, nesne yuklendikten sonra ama
    // SaveChanges'ten once, disaridan yapilan bir UPDATE ile uretiliyor.
    // Bunu yapmadan yazilan test yesil yanar ama hicbir sey kanitlamaz
    // (olculdu: AzamiDeneme = 1 yapilinca da geciyordu).
    [Fact]
    public async Task Yukleme_ile_kayit_arasinda_satir_degisirse_handler_yeniden_dener()
    {
        await AgvDurumunuSifirlaAsync();

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var notifier = kapsam.ServiceProvider.GetRequiredService<IFleetNotifier>();

        var cagri = 0;

        var sonuc = await AgvGuncelleyici.GuncelleAsync(db, notifier, Agv01, agv =>
        {
            cagri++;

            if (cagri == 1)
            {
                // Telemetrinin yaptigi sey: ayni satiri baska bir baglantidan
                // guncelle, xmin ilerlesin.
                BataryaDegistirAsync(77).GetAwaiter().GetResult();
            }

            return agv.Mesgullestir().IsSuccess;
        }, CancellationToken.None);

        Assert.True(sonuc);
        Assert.Equal(2, cagri);
        Assert.Equal(AgvStatus.Busy, (await AgvOkuAsync()).Status);

        await AgvDurumunuSifirlaAsync();
    }

    [Fact]
    public async Task Telemetri_araya_girse_de_atama_olayi_teslim_edilir()
    {
        await AgvDurumunuSifirlaAsync();
        var baslangic = DateTime.UtcNow;
        var istemci = await fabrika.IstemciAsync();
        var gorevId = await GorevOlusturAsync(istemci);

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        // Handler nesneyi yukledikten SONRA satiri disaridan degistirmenin
        // guvenilir yolu: dagitici calismadan hemen once xmin'i ilerlet.
        // Tek bir yazma yerine birkac tur, yarisin gercekten yakalanmasi icin.
        for (var i = 0; i < 3; i++)
        {
            await BataryaDegistirAsync(80 + i);
        }

        await DagiticiCalistirAsync();

        var agv = await AgvOkuAsync();
        Assert.Equal(AgvStatus.Busy, agv.Status);

        var hata = await OutboxHatasiAsync(baslangic);
        Assert.True(string.IsNullOrEmpty(hata), $"Outbox hatasi: {hata}");

        await TemizleAsync(istemci, gorevId);
    }

    [Fact]
    public async Task Handler_kendi_yazdigi_degisikligi_kaybetmez()
    {
        // Yeniden deneme sirasinda takipteki eski nesne ayrilmazsa sonraki
        // deneme de ayni xmin ile gider ve sonsuza kadar catisir.
        await AgvDurumunuSifirlaAsync();
        var istemci = await fabrika.IstemciAsync();
        var gorevId = await GorevOlusturAsync(istemci);

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        await DagiticiCalistirAsync();

        Assert.Equal(AgvStatus.Busy, (await AgvOkuAsync()).Status);

        await istemci.PostAsync($"/api/tasks/{gorevId}/start", null);
        await istemci.PostAsync($"/api/tasks/{gorevId}/complete", null);
        await BataryaDegistirAsync(55);
        await DagiticiCalistirAsync();

        Assert.Equal(AgvStatus.Available, (await AgvOkuAsync()).Status);
    }

    private static async Task<Guid> GorevOlusturAsync(HttpClient istemci)
    {
        var yanit = await istemci.PostAsJsonAsync("/api/tasks", new
        {
            fromLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            toLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
            materialCode = $"ESZ-{Guid.NewGuid():N}"[..12],
            quantity = 1,
            priority = 1,
        });

        return (await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
    }

    private async Task TemizleAsync(HttpClient istemci, Guid gorevId)
    {
        await istemci.PostAsync($"/api/tasks/{gorevId}/start", null);
        await istemci.PostAsync($"/api/tasks/{gorevId}/complete", null);
        await DagiticiCalistirAsync();
    }

    private async Task DagiticiCalistirAsync()
    {
        var dagitici = fabrika.Services.GetServices<IHostedService>()
            .OfType<OutboxDispatcher>().Single();

        await dagitici.BirTurCalistirAsync(CancellationToken.None);
    }

    private async Task BataryaDegistirAsync(int batarya)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        await db.Database.ExecuteSqlAsync(
            $"UPDATE fleet.agv SET battery_level = {batarya} WHERE id = {Agv01}");
    }

    private async Task<Agv> AgvOkuAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        return await db.Agvs.AsNoTracking().SingleAsync(a => a.Id == Agv01);
    }

    private async Task AgvDurumunuSifirlaAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        await db.Database.ExecuteSqlAsync(
            $"UPDATE fleet.agv SET status = 'Available', battery_level = 95 WHERE id = {Agv01}");
    }

    // Sadece BU testin urettigi olaylara bak: paylasilan veritabaninda baska
    // testlerin biraktigi hatali satirlar da var.
    private async Task<string?> OutboxHatasiAsync(DateTime baslangic)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<Tasks.Persistence.TasksDbContext>();
        return await db.OutboxMessages
            .Where(o => o.OccurredAtUtc >= baslangic && o.ProcessedAtUtc == null && o.Error != null)
            .OrderByDescending(o => o.OccurredAtUtc)
            .Select(o => o.Error)
            .FirstOrDefaultAsync();
    }

    private sealed record OlusturmaYaniti(Guid Id);
}
