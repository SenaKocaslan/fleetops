using System.Net.Http.Json;
using FleetOps.Api;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class AlarmGruplamaTests(FleetOpsApiFactory fabrika)
{
    private const string BeklemeKodu = "Tasks.UzunSureBekleyenGorev";
    private const string OluMektupKodu = "Tasks.TeslimEdilemeyenOlay";
    private static readonly Guid Kabul = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid RafA1 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    [Fact]
    public async Task Ayni_turden_cok_alarm_tek_grupta_toplanir_ve_ornekler_sinirlanir()
    {
        var etiket = $"GRP-{Guid.NewGuid():N}"[..12];
        var adet = AlarmUcNoktalari.OrnekSiniri + 2;

        // Her biri farkli sure bekliyor; en eskisi (en uzun bekleyen) once
        // gorunmeli -- kaynak boyle siraliyor ve uc nokta bunu korumali.
        for (var i = 0; i < adet; i++)
        {
            var id = await GorevOlusturAsync($"{etiket}-{i}");
            await GeriyeTarihleAsync(id, TimeSpan.FromMinutes(20 + i));
        }

        try
        {
            var yanit = await AlarmlarAsync();

            var grup = Assert.Single(yanit.Groups, g => g.Code == BeklemeKodu);
            Assert.True(grup.Count >= adet, $"grup adedi {grup.Count}");
            Assert.Equal(AlarmUcNoktalari.OrnekSiniri, grup.Shown);

            var ornekler = yanit.Items.Where(a => a.Code == BeklemeKodu).ToList();
            Assert.Equal(AlarmUcNoktalari.OrnekSiniri, ornekler.Count);

            // En uzun bekleyen: $"{etiket}-{adet - 1}" (20 + adet - 1 dakika).
            Assert.Equal($"{etiket}-{adet - 1}", ornekler[0].Subject);
        }
        finally
        {
            await EtiketliGorevleriSilAsync(etiket);
        }
    }

    [Fact]
    public async Task Kritik_rozeti_gosterilen_ornek_sayisini_degil_gercek_sayiyi_verir()
    {
        var adet = AlarmUcNoktalari.OrnekSiniri + 3;
        var kimlikler = new List<Guid>();
        for (var i = 0; i < adet; i++)
        {
            kimlikler.Add(await OluMektupEkleAsync());
        }

        try
        {
            var yanit = await AlarmlarAsync();

            var grup = Assert.Single(yanit.Groups, g => g.Code == OluMektupKodu);
            Assert.Equal(AlarmUcNoktalari.OrnekSiniri, grup.Shown);
            Assert.True(grup.Count >= adet);

            // Rozet kesilseydi operator 5 kritik alarm var sanardi.
            Assert.True(yanit.CriticalCount >= adet, $"rozet {yanit.CriticalCount}");

            // Kritik grup, uyari selinin altinda kalmamali.
            Assert.Equal("Kritik", yanit.Groups[0].Severity);
        }
        finally
        {
            await OluMektuplariSilAsync(kimlikler);
        }
    }

    private async Task<AlarmYaniti> AlarmlarAsync() =>
        (await (await fabrika.IstemciAsync()).GetFromJsonAsync<AlarmYaniti>("/api/alarms"))!;

    private async Task<Guid> GorevOlusturAsync(string malzeme)
    {
        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            "/api/tasks", new CreateTaskCommand(Kabul, RafA1, malzeme, 1, 1));
        yanit.EnsureSuccessStatusCode();
        return (await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
    }

    private async Task GeriyeTarihleAsync(Guid id, TimeSpan sure)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE tasks.transport_task SET created_at_utc = {DateTime.UtcNow - sure} WHERE id = {id}");
    }

    private async Task EtiketliGorevleriSilAsync(string etiket)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.TransportTasks.Where(t => t.MaterialCode.StartsWith(etiket)).ExecuteDeleteAsync();
    }

    private async Task<Guid> OluMektupEkleAsync()
    {
        var id = Guid.NewGuid();
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tasks.outbox_message
                (id, type, payload, occurred_at_utc, attempt_count, dead_lettered_at_utc, error)
            VALUES ({id}, 'GruplamaTesti', {"{}"}::jsonb, {DateTime.UtcNow}, 5, {DateTime.UtcNow}, 'test')
            """);
        return id;
    }

    private async Task OluMektuplariSilAsync(List<Guid> kimlikler)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.OutboxMessages.Where(m => kimlikler.Contains(m.Id)).ExecuteDeleteAsync();
    }

    private sealed record AlarmYaniti(
        List<AlarmKalemi> Items, List<AlarmGrubu> Groups, int TotalCount, int CriticalCount);

    private sealed record AlarmKalemi(string Code, string Severity, string Subject, string Message);

    private sealed record AlarmGrubu(string Code, string Severity, int Count, int Shown);

    private sealed record OlusturmaYaniti(Guid Id);
}
