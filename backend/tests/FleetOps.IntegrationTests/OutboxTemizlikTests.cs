using FleetOps.IntegrationTests.Altyapi;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Infrastructure;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class OutboxTemizlikTests(FleetOpsApiFactory fabrika)
{
    [Fact]
    public async Task Yalnizca_saklama_suresini_asan_islenmis_mesajlar_silinir()
    {
        var saklama = fabrika.Services.GetRequiredService<IOptions<OutboxOptions>>().Value.Retention;
        var simdi = DateTime.UtcNow;
        var eski = simdi - saklama - TimeSpan.FromDays(1);

        var eskiIslenmis = await MesajEkleAsync(islendi: eski);
        var yeniIslenmis = await MesajEkleAsync(islendi: simdi - TimeSpan.FromMinutes(5));
        var bekleyen = await MesajEkleAsync(islendi: null);

        // Eski ama OLU MEKTUP: silinirse "teslim edilemeyen olay" alarmi da
        // kaybolur ve sorun hic yasanmamis gibi gorunur.
        var oluMektup = await MesajEkleAsync(islendi: null, oluMektup: eski);

        var silinen = await TemizleyiciCalistirAsync();

        Assert.True(silinen >= 1);

        var kalanlar = await KalanlarAsync(eskiIslenmis, yeniIslenmis, bekleyen, oluMektup);

        Assert.DoesNotContain(eskiIslenmis, kalanlar);
        Assert.Contains(yeniIslenmis, kalanlar);
        Assert.Contains(bekleyen, kalanlar);
        Assert.Contains(oluMektup, kalanlar);
    }

    [Fact]
    public void Temizleyici_barindirilan_servis_olarak_kayitli()
    {
        Assert.Single(fabrika.Services.GetServices<IHostedService>().OfType<OutboxTemizleyici>());
    }

    private Task<int> TemizleyiciCalistirAsync() =>
        fabrika.Services.GetServices<IHostedService>()
            .OfType<OutboxTemizleyici>().Single()
            .BirTurCalistirAsync(CancellationToken.None);

    // Mesaji dogrudan tabloya yaziyoruz: gercek akistan uretmek bir gorev,
    // bir musait arac ve bir atama gerektirirdi; burada sinanan sey yalnizca
    // temizleyicinin hangi satirlari sectigi.
    private async Task<Guid> MesajEkleAsync(DateTime? islendi, DateTime? oluMektup = null)
    {
        var id = Guid.NewGuid();

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tasks.outbox_message
                (id, type, payload, occurred_at_utc, processed_at_utc, attempt_count, dead_lettered_at_utc)
            VALUES
                ({id}, 'TemizlikTesti', {"{}"}::jsonb, {DateTime.UtcNow.AddDays(-30)},
                 {islendi}, {(oluMektup is null ? 0 : 5)}, {oluMektup})
            """);

        return id;
    }

    private async Task<List<Guid>> KalanlarAsync(params Guid[] kimlikler)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        return await db.OutboxMessages
            .Where(m => kimlikler.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync();
    }
}
