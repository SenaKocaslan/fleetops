using System.Net;
using System.Net.Http.Json;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class AlarmTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv02 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Alarm_uc_noktasi_token_ister()
    {
        var yanit = await fabrika.CreateClient().GetAsync("/api/alarms");

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact]
    public async Task Dusuk_batarya_uyari_uretir()
    {
        await BataryaAyarlaAsync(Agv02, 25);

        var alarmlar = await AlarmlariGetirAsync();
        var alarm = Assert.Single(alarmlar.Items, a => a.Subject == "AGV-02" && a.Code == "Fleet.DusukBatarya");

        Assert.Equal("Uyari", alarm.Severity);
    }

    [Fact]
    public async Task Kritik_batarya_uyari_yerine_kritik_uretir()
    {
        await BataryaAyarlaAsync(Agv02, 10);

        var alarmlar = await AlarmlariGetirAsync();

        // Ikisi birden uretilirse ayni sorun iki kez rapor edilirdi.
        Assert.Single(alarmlar.Items, a => a.Subject == "AGV-02" && a.Code == "Fleet.KritikBatarya");
        Assert.DoesNotContain(alarmlar.Items, a => a.Subject == "AGV-02" && a.Code == "Fleet.DusukBatarya");
        Assert.True(alarmlar.CriticalCount > 0);
    }

    [Fact]
    public async Task Saglikli_batarya_alarm_uretmez()
    {
        await BataryaAyarlaAsync(Agv02, 90);

        var alarmlar = await AlarmlariGetirAsync();

        Assert.DoesNotContain(alarmlar.Items, a => a.Subject == "AGV-02" && a.Code.StartsWith("Fleet.") && a.Code.Contains("Batarya"));
    }

    [Fact]
    public async Task Hic_telemetri_gondermemis_arac_sessizlik_alarmi_uretmez()
    {
        // "Hic gormedim" ile "uzun suredir gormuyorum" ayni sey degil.
        await SonGorulmeAyarlaAsync(Agv02, null);

        var alarmlar = await AlarmlariGetirAsync();

        Assert.DoesNotContain(alarmlar.Items, a => a.Subject == "AGV-02" && a.Code == "Fleet.TelemetriKesildi");
    }

    [Fact]
    public async Task Uzun_suredir_susan_arac_kritik_alarm_uretir()
    {
        await SonGorulmeAyarlaAsync(Agv02, DateTime.UtcNow.AddHours(-1));

        var alarmlar = await AlarmlariGetirAsync();
        var alarm = Assert.Single(alarmlar.Items, a => a.Subject == "AGV-02" && a.Code == "Fleet.TelemetriKesildi");

        Assert.Equal("Kritik", alarm.Severity);

        await SonGorulmeAyarlaAsync(Agv02, DateTime.UtcNow);
    }

    [Fact]
    public async Task Suresi_dolmus_ama_birakilmamis_kilit_alarm_uretir()
    {
        var kaynakId = await TakiliKilitOlusturAsync();

        var alarmlar = await AlarmlariGetirAsync();
        var alarm = Assert.Single(
            alarmlar.Items, a => a.Code == "Tasks.TakiliKilit" && a.Subject == kaynakId.ToString());

        Assert.Equal("Kritik", alarm.Severity);

        await KilitleriTemizleAsync();
    }

    [Fact]
    public async Task Alarmlar_iki_modulden_de_toplanir()
    {
        await BataryaAyarlaAsync(Agv02, 10);
        var kaynakId = await TakiliKilitOlusturAsync();

        var alarmlar = await AlarmlariGetirAsync();

        // Modul sinirini gecen tek yer composition root; iki kaynak da
        // ayni listede gorunmeli.
        Assert.Contains(alarmlar.Items, a => a.Code.StartsWith("Fleet."));
        Assert.Contains(alarmlar.Items, a => a.Code.StartsWith("Tasks."));

        await KilitleriTemizleAsync();
        await BataryaAyarlaAsync(Agv02, 60);
        _ = kaynakId;
    }

    [Fact]
    public async Task Kritik_alarmlar_listenin_basinda_gelir()
    {
        await BataryaAyarlaAsync(Agv02, 10);

        var alarmlar = await AlarmlariGetirAsync();
        var siddetler = alarmlar.Items.Select(a => a.Severity switch
        {
            "Kritik" => 2,
            "Uyari" => 1,
            _ => 0,
        }).ToList();

        Assert.Equal(siddetler.OrderByDescending(s => s), siddetler);

        await BataryaAyarlaAsync(Agv02, 60);
    }

    private async Task<AlarmYaniti> AlarmlariGetirAsync()
    {
        var istemci = await fabrika.IstemciAsync();
        return (await istemci.GetFromJsonAsync<AlarmYaniti>("/api/alarms"))!;
    }

    private async Task BataryaAyarlaAsync(Guid id, int batarya)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var agv = await db.Agvs.SingleAsync(a => a.Id == id);
        agv.BataryaBildir(batarya);
        await db.SaveChangesAsync();
    }

    private async Task SonGorulmeAyarlaAsync(Guid id, DateTime? an)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        await db.Database.ExecuteSqlAsync(
            $"UPDATE fleet.agv SET last_seen_at_utc = {an} WHERE id = {id}");
    }

    private async Task<Guid> TakiliKilitOlusturAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        await KilitleriTemizleAsync();

        var kaynak = await db.Resources.AsNoTracking().FirstAsync();
        var kilit = ResourceLock.Acquire(
            Guid.NewGuid(), kaynak.Id, Agv02, DateTime.UtcNow.AddHours(-2), TimeSpan.FromMinutes(5));

        db.ResourceLocks.Add(kilit.Value);
        await db.SaveChangesAsync();

        return kaynak.Id;
    }

    private async Task KilitleriTemizleAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM tasks.resource_lock");
    }

    private sealed record AlarmYaniti(List<AlarmKalemi> Items, int CriticalCount);

    private sealed record AlarmKalemi(
        string Code, string Severity, string Subject, string Message, DateTime DetectedAtUtc);
}
