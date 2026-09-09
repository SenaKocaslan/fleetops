using System.Net.Http.Json;
using FleetOps.Fleet.Application;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.Domain;
using FleetOps.SharedKernel.IntegrationEvents;
using FleetOps.Stock.Application;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Infrastructure;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FleetOps.IntegrationTests;

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

        await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        var mesaj = await db.OutboxMessages.SingleAsync(m => m.ProcessedAtUtc == null);
        Assert.Equal(nameof(TaskAssignedIntegrationEvent), mesaj.Type);

        // Iki satirin xmin degeri esitse ayni transaction'da yazilmislardir.
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
        var istemci = await fabrika.IstemciAsync();

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

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
        var istemci = await fabrika.IstemciAsync();

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        await istemci.PostAsync($"/api/tasks/{gorevId}/start", null);
        await istemci.PostAsync($"/api/tasks/{gorevId}/complete", null);

        await DagiticiCalistirAsync();

        var hareketler = (await istemci.GetFromJsonAsync<PagedResult<StockMovementSummary>>(
            "/api/stock/movements?pageSize=100"))!.Items;
        var hareket = Assert.Single(hareketler, h => h.SourceTaskId == gorevId);

        Assert.Equal(3, hareket.Quantity);
        Assert.Equal("KABUL-01", hareket.FromLocationCode);
        Assert.Equal("RAF-A1", hareket.ToLocationCode);

        Assert.Equal("Available", (await AgvAsync()).Status);
    }

    [Fact]
    public async Task Ayni_olay_iki_kez_teslim_edilse_de_tek_stok_hareketi_olusur()
    {
        await OutboxuTemizleAsync();
        await AgvDurumunuSifirlaAsync();
        var gorevId = await GorevOlusturAsync();
        var istemci = await fabrika.IstemciAsync();

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        await istemci.PostAsync($"/api/tasks/{gorevId}/start", null);
        await istemci.PostAsync($"/api/tasks/{gorevId}/complete", null);

        await DagiticiCalistirAsync();

        await IsaretleriGeriAlAsync();
        await DagiticiCalistirAsync();

        var hareketler = (await istemci.GetFromJsonAsync<PagedResult<StockMovementSummary>>(
            "/api/stock/movements?pageSize=100"))!.Items;

        Assert.Single(hareketler, h => h.SourceTaskId == gorevId);

        await AgvDurumunuSifirlaAsync();
    }

    [Fact]
    public async Task Islenmis_mesaj_ikinci_turda_tekrar_islenmez()
    {
        await OutboxuTemizleAsync();
        var gorevId = await GorevOlusturAsync();

        await (await fabrika.IstemciAsync()).PostAsJsonAsync(
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

        await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

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
        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            "/api/tasks", new CreateTaskCommand(Kabul, RafA1, "MLZ-OUTBOX", 3, 1));

        yanit.EnsureSuccessStatusCode();
        return (await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
    }

    private async Task<AgvSummary> AgvAsync()
    {
        var agvler = await (await fabrika.IstemciAsync()).GetFromJsonAsync<List<AgvSummary>>("/api/agvs");
        return agvler!.Single(a => a.Id == Agv01);
    }

    private async Task OutboxuTemizleAsync()
    {
        await fabrika.FiloyuHazirlaAsync();

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

    [Fact]
    public async Task Surekli_basarisiz_mesaj_azami_denemeden_sonra_kuyruktan_cikar()
    {
        await OutboxuTemizleAsync();
        var azami = AzamiDeneme();
        await BozukMesajUretAsync();

        // Her tur mesaji bir kez daha deniyor; sonuncusunda sinir doluyor.
        for (var tur = 1; tur <= azami; tur++)
        {
            Assert.Equal(0, await DagiticiCalistirAsync());

            var ara = await BozukMesajiOkuAsync();
            Assert.Equal(tur, ara.AttemptCount);
            Assert.Equal(tur == azami, ara.DeadLetteredAtUtc is not null);
        }

        // Sinir dolduktan sonra dagitici mesaji BIR DAHA ALMIYOR: sayac
        // artmiyorsa mesaj gercekten kuyruktan cikmis demektir.
        await DagiticiCalistirAsync();
        await DagiticiCalistirAsync();

        var son = await BozukMesajiOkuAsync();
        Assert.Equal(azami, son.AttemptCount);
        Assert.NotNull(son.DeadLetteredAtUtc);

        // Satir silinmiyor: hata metni tanilama icin duruyor.
        Assert.Null(son.ProcessedAtUtc);
        Assert.Contains("OlmayanOlay", son.Error);

        await OutboxuTemizleAsync();
    }

    [Fact]
    public async Task Saglam_mesaj_olu_mektup_isaretlenmeden_islenir()
    {
        // Kontrol testi: yukaridaki test, dagitici her mesaji olu mektuba
        // tasisaydi da yesil yanardi.
        await OutboxuTemizleAsync();
        await AgvDurumunuSifirlaAsync();
        var gorevId = await GorevOlusturAsync();

        await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        Assert.True(await DagiticiCalistirAsync() >= 1);

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        var mesaj = await db.OutboxMessages.FirstAsync();

        Assert.Null(mesaj.DeadLetteredAtUtc);
        Assert.Equal(0, mesaj.AttemptCount);
        Assert.NotNull(mesaj.ProcessedAtUtc);

        await AgvDurumunuSifirlaAsync();
    }

    [Fact]
    public async Task Olu_mektup_kritik_alarm_uretir()
    {
        await OutboxuTemizleAsync();
        await BozukMesajUretAsync();

        for (var tur = 0; tur < AzamiDeneme(); tur++)
        {
            await DagiticiCalistirAsync();
        }

        var yanit = await (await fabrika.IstemciAsync()).GetFromJsonAsync<AlarmYaniti>("/api/alarms");

        var alarm = Assert.Single(yanit!.Items, a => a.Code == "Tasks.TeslimEdilemeyenOlay");
        Assert.Equal("Kritik", alarm.Severity);
        Assert.Contains("OlmayanOlay", alarm.Message);
        Assert.True(yanit.CriticalCount >= 1);

        await OutboxuTemizleAsync();
    }

    private int AzamiDeneme() =>
        fabrika.Services.GetRequiredService<IOptions<OutboxOptions>>().Value.MaxAttempts;

    // Turu cozumlenemeyen bir mesaj her turda ayni sekilde patlar; olu
    // mektuba giden yolu deterministik olarak izlemenin en sade yolu bu.
    private async Task BozukMesajUretAsync()
    {
        var gorevId = await GorevOlusturAsync();

        await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Type, "OlmayanOlay"));
    }

    private async Task<OutboxMessage> BozukMesajiOkuAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        return await db.OutboxMessages.AsNoTracking().FirstAsync(m => m.Type == "OlmayanOlay");
    }

    private sealed record AlarmYaniti(List<AlarmKalemi> Items, int CriticalCount);

    private sealed record AlarmKalemi(string Code, string Severity, string Subject, string Message);

    private sealed record OlusturmaYaniti(Guid Id);
}
