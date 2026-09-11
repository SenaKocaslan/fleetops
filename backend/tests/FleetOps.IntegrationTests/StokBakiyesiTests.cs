using System.Net.Http.Json;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.Domain;
using FleetOps.Stock.Application;
using FleetOps.Stock.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class StokBakiyesiTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Kabul = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid RafA1 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");
    private static readonly Guid RafB2 = Guid.Parse("cccccccc-0000-0000-0000-000000000003");
    private static readonly Guid Sevk = Guid.Parse("cccccccc-0000-0000-0000-000000000004");

    [Fact]
    public async Task Raf_bakiyesi_giris_eksi_cikis_olarak_hesaplanir()
    {
        var malzeme = YeniMalzeme();
        await HareketAsync(malzeme, 5, Kabul, RafA1);
        await HareketAsync(malzeme, 2, RafA1, RafB2);
        await HareketAsync(malzeme, 1, RafB2, Sevk);

        var bakiyeler = await BakiyelerAsync(malzeme);

        Assert.Equal(3, Assert.Single(bakiyeler, b => b.LocationCode == "RAF-A1").Quantity);
        Assert.Equal(1, Assert.Single(bakiyeler, b => b.LocationCode == "RAF-B2").Quantity);
    }

    [Fact]
    public async Task Sinir_bolgeleri_listelenmez()
    {
        // Kabul'e disaridan ne geldigini sistem bilmiyor; oradaki bakiye
        // hareketlerden hesaplanamaz (hep eksi cikardi).
        var malzeme = YeniMalzeme();
        await HareketAsync(malzeme, 4, Kabul, RafA1);
        await HareketAsync(malzeme, 4, RafA1, Sevk);

        var bakiyeler = await BakiyelerAsync(malzeme);

        Assert.DoesNotContain(bakiyeler, b => b.LocationCode is "KABUL-01" or "SEVK-01");
    }

    [Fact]
    public async Task Sifirlanan_bakiye_listelenmez()
    {
        var malzeme = YeniMalzeme();
        await HareketAsync(malzeme, 3, Kabul, RafA1);
        await HareketAsync(malzeme, 3, RafA1, Sevk);

        Assert.Empty(await BakiyelerAsync(malzeme));
    }

    [Fact]
    public async Task Raftaki_eksi_bakiye_alarm_uretir()
    {
        var malzeme = YeniMalzeme();

        // RAF-B2'ye hic giris yok ama cikis var: kayit ile gercek ayrismis.
        await HareketAsync(malzeme, 2, RafB2, Sevk);

        var yanit = await (await fabrika.IstemciAsync())
            .GetFromJsonAsync<AlarmYaniti>("/api/alarms");

        var alarm = Assert.Single(yanit!.Items, a => a.Subject == $"RAF-B2 / {malzeme}");
        Assert.Equal("Stock.EksiBakiye", alarm.Code);
        Assert.Contains("-2", alarm.Message);
    }

    [Fact]
    public async Task Pozitif_bakiye_alarm_uretmez()
    {
        // Kontrol testi: kaynak her bakiyeyi alarm sayiyor olsaydi yukaridaki
        // test de yesil yanardi.
        var malzeme = YeniMalzeme();
        await HareketAsync(malzeme, 2, Kabul, RafA1);

        var yanit = await (await fabrika.IstemciAsync())
            .GetFromJsonAsync<AlarmYaniti>("/api/alarms");

        Assert.DoesNotContain(yanit!.Items, a => a.Subject.EndsWith(malzeme, StringComparison.Ordinal));
    }

    private static string YeniMalzeme() => $"BKY-{Guid.NewGuid():N}"[..16];

    private async Task<IReadOnlyList<StockBalanceSummary>> BakiyelerAsync(string malzeme)
    {
        var sayfa = await (await fabrika.IstemciAsync())
            .GetFromJsonAsync<PagedResult<StockBalanceSummary>>(
                $"/api/stock/balances?materialCode={malzeme}&pageSize=100");
        return sayfa!.Items;
    }

    // Hareket dogrudan yaziliyor: gercek akistan uretmek gorev, arac, atama
    // ve outbox turu gerektirirdi; burada sinanan yalnizca bakiye hesabi.
    private async Task HareketAsync(string malzeme, int miktar, Guid nereden, Guid nereye)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<StockDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO stock.stock_movement
                (id, material_code, quantity, from_location_id, to_location_id, source_task_id, moved_at_utc)
            VALUES ({Guid.NewGuid()}, {malzeme}, {miktar}, {nereden}, {nereye}, {Guid.NewGuid()}, {DateTime.UtcNow})
            """);
    }

    private sealed record AlarmYaniti(List<AlarmKalemi> Items, int CriticalCount);

    private sealed record AlarmKalemi(string Code, string Severity, string Subject, string Message);
}
