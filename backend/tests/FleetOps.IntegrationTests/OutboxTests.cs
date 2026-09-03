using System.Net.Http.Json;
using FleetOps.Fleet.Application;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.IntegrationEvents;
using FleetOps.Stock.Application;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Infrastructure;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FleetOps.IntegrationTests;

// Outbox + integration event zinciri: olay durum degisikligiyle ayni
// transaction'da yazilir, dagitici teslim eder, tuketiciler kendi
// modullerinde is yapar.
[Collection(VeritabaniKoleksiyonu.Ad)]
public class OutboxTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kabul = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid RafA1 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    [Fact]
    public async Task Olay_durum_degisikligiyle_ayni_transactionda_yazilir()
    {
        await OutboxuTemizleAsync();
        var gorevId = await GorevOlusturAsync();

        await fabrika.CreateClient().PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        var mesaj = await db.OutboxMessages.SingleAsync(m => m.ProcessedAtUtc == null);
        Assert.Equal(nameof(TaskAssignedIntegrationEvent), mesaj.Type);

        // PostgreSQL her satirda onu yazan transaction'in kimligini tutar.
        // Iki satirin xmin degeri esitse ayni transaction'da yazilmislardir -
        // outbox'in tum varlik sebebi bu.
        var gorevXmin = await XminAsync(db, "tasks.transport_task", gorevId);
        var mesajXmin = await XminAsync(db, "tasks.outbox_message", mesaj.Id);

        Assert.Equal(gorevXmin, mesajXmin);
    }

    [Fact]
    public async Task Dagitici_atama_olayini_teslim_eder_ve_agv_mesgullesir()
    {
        await OutboxuTemizleAsync();
        await AgvDurumunuSifirlaAsync();
        var gorevId = await GorevOlusturAsync();
        var istemci = fabrika.CreateClient();

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        // Teslimden once AGV hala musait: olay yazildi ama daha gitmedi.
        Assert.True((await AgvAsync()).GorevAlabilir);

        var islenen = await DagiticiCalistirAsync();

        Assert.True(islenen >= 1);
        Assert.False((await AgvAsync()).GorevAlabilir);
        Assert.Equal("Busy", (await AgvAsync()).Status);

        await AgvDurumunuSifirlaAsync();
    }

    [Fact]
    public async Task Gorev_tamamlaninca_stok_hareketi_olusur_ve_agv_serbest_kalir()
    {
        await OutboxuTemizleAsync();
        await AgvDurumunuSifirlaAsync();
        var gorevId = await GorevOlusturAsync();
        var istemci = fabrika.CreateClient();

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        await istemci.PostAsync($"/api/tasks/{gorevId}/start", null);
        await istemci.PostAsync($"/api/tasks/{gorevId}/complete", null);

        await DagiticiCalistirAsync();

        var hareketler = await istemci.GetFromJsonAsync<List<StockMovementSummary>>(
            "/api/stock/movements");
        var hareket = Assert.Single(hareketler!, h => h.SourceTaskId == gorevId);

        Assert.Equal(3, hareket.Quantity);
        Assert.Equal("KABUL-01", hareket.FromLocationCode);
        Assert.Equal("RAF-A1", hareket.ToLocationCode);

        // Fleet de ayni olayi dinliyor: AGV yeniden musait.
        Assert.Equal("Available", (await AgvAsync()).Status);
    }

    [Fact]
    public async Task Ayni_olay_iki_kez_teslim_edilse_de_tek_stok_hareketi_olusur()
    {
        await OutboxuTemizleAsync();
        await AgvDurumunuSifirlaAsync();
        var gorevId = await GorevOlusturAsync();
        var istemci = fabrika.CreateClient();

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        await istemci.PostAsync($"/api/tasks/{gorevId}/start", null);
        await istemci.PostAsync($"/api/tasks/{gorevId}/complete", null);

        await DagiticiCalistirAsync();

        // Teslimat en az bir kez: dagitici isaretlemeden once cokerse ayni
        // olay tekrar gelir. Islenmis isaretini geri alarak bunu taklit
        // ediyoruz.
        await IsaretleriGeriAlAsync();
        await DagiticiCalistirAsync();

        var hareketler = await istemci.GetFromJsonAsync<List<StockMovementSummary>>(
            "/api/stock/movements");

        Assert.Single(hareketler!, h => h.SourceTaskId == gorevId);

        await AgvDurumunuSifirlaAsync();
    }

    [Fact]
    public async Task Islenmis_mesaj_ikinci_turda_tekrar_islenmez()
    {
        await OutboxuTemizleAsync();
        var gorevId = await GorevOlusturAsync();

        await fabrika.CreateClient().PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        var ilk = await DagiticiCalistirAsync();
        var ikinci = await DagiticiCalistirAsync();

        Assert.True(ilk >= 1);
        Assert.Equal(0, ikinci);

        await AgvDurumunuSifirlaAsync();
    }

    [Fact]
    public async Task Cozumlenemeyen_tur_islenmis_isaretlenmez_ve_hata_kaydedilir()
    {
        await OutboxuTemizleAsync();
        var gorevId = await GorevOlusturAsync();

        await fabrika.CreateClient().PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        // Sozlesmesi kaldirilmis bir olay turunu taklit et.
        using (var kapsam = fabrika.KapsamAc())
        {
            var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
            await db.OutboxMessages
                .Where(m => m.ProcessedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Type, "OlmayanOlay"));
        }

        var islenen = await DagiticiCalistirAsync();

        Assert.Equal(0, islenen);

        using var kontrol = fabrika.KapsamAc();
        var db2 = kontrol.ServiceProvider.GetRequiredService<TasksDbContext>();
        var mesaj = await db2.OutboxMessages.FirstAsync(m => m.Type == "OlmayanOlay");

        // Islenmis isaretlenmedi: bir sonraki turda tekrar denenecek.
        Assert.Null(mesaj.ProcessedAtUtc);
        Assert.Contains("OlmayanOlay", mesaj.Error);
    }

    [Fact]
    public void Dagitici_barindirilan_servis_olarak_kayitli()
    {
        var servisler = fabrika.Services.GetServices<IHostedService>();

        Assert.Single(servisler.OfType<OutboxDispatcher>());
    }

    private async Task<int> DagiticiCalistirAsync()
    {
        var dagitici = fabrika.Services.GetServices<IHostedService>()
            .OfType<OutboxDispatcher>().Single();

        return await dagitici.BirTurCalistirAsync(CancellationToken.None);
    }

    private async Task<Guid> GorevOlusturAsync()
    {
        var yanit = await fabrika.CreateClient().PostAsJsonAsync(
            "/api/tasks", new CreateTaskCommand(Kabul, RafA1, "MLZ-OUTBOX", 3, 1));

        yanit.EnsureSuccessStatusCode();
        return (await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
    }

    private async Task<AgvSummary> AgvAsync()
    {
        var agvler = await fabrika.CreateClient().GetFromJsonAsync<List<AgvSummary>>("/api/agvs");
        return agvler!.Single(a => a.Id == Agv01);
    }

    private async Task OutboxuTemizleAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.OutboxMessages.ExecuteDeleteAsync();
    }

    private async Task IsaretleriGeriAlAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.OutboxMessages.ExecuteUpdateAsync(
            s => s.SetProperty(m => m.ProcessedAtUtc, (DateTime?)null));
    }

    private async Task AgvDurumunuSifirlaAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var agv = await db.Agvs.SingleAsync(a => a.Id == Agv01);
        agv.ServiseAl();
        await db.SaveChangesAsync();
    }

    private static async Task<long> XminAsync(TasksDbContext db, string tablo, Guid id)
    {
        // Tablo adi kod icinde sabit, kimlik parametre olarak gidiyor.
        var sql = "SELECT xmin::text::bigint AS \"Value\" FROM " + tablo + " WHERE id = {0}";

        var sonuc = await db.Database.SqlQueryRaw<long>(sql, id).ToListAsync();

        return sonuc.Single();
    }

    private sealed record OlusturmaYaniti(Guid Id);
}
