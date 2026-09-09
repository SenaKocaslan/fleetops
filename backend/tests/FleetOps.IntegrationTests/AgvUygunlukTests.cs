using System.Net;
using System.Net.Http.Json;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.Tasks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class AgvUygunlukTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kabul = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid RafA1 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");
    private static readonly Guid Dock = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    [Fact]
    public async Task Sarjdaki_araca_gorev_atanamaz()
    {
        await fabrika.FiloyuHazirlaAsync();
        await DurumAyarlaAsync(sarjda: true);
        var gorevId = await GorevOlusturAsync();

        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
        var hata = await yanit.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Task.AgvGorevAlamaz", hata?.Code);
        Assert.Contains("AGV-01", hata!.Message);
    }

    [Fact]
    public async Task Musait_araca_gorev_atanabilir()
    {
        // Kontrol testi: kural "hicbir araca atanamaz" seklinde yanlis
        // uygulansaydi yukaridaki test de yesil yanardi.
        await fabrika.FiloyuHazirlaAsync();
        var gorevId = await GorevOlusturAsync();

        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);
    }

    [Fact]
    public async Task Bataryasi_esigin_altindaki_araca_gorev_atanamaz()
    {
        await fabrika.FiloyuHazirlaAsync();
        await DurumAyarlaAsync(batarya: 5);
        var gorevId = await GorevOlusturAsync();

        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
    }

    [Fact]
    public async Task Olmayan_araca_gorev_atanamaz()
    {
        await fabrika.FiloyuHazirlaAsync();
        var gorevId = await GorevOlusturAsync();

        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
    }

    [Fact]
    public async Task Sarjdaki_arac_kaynak_kilidi_alamaz()
    {
        await fabrika.FiloyuHazirlaAsync();
        await KilitleriTemizleAsync();
        await DurumAyarlaAsync(sarjda: true);

        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/resources/{Dock}/lock", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
        var hata = await yanit.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Resource.AgvSahadaDegil", hata?.Code);
    }

    [Fact]
    public async Task Mesgul_arac_kaynak_kilidi_ALABILIR()
    {
        // Kilit gorev YURUTULURKEN alinir. Kurali "yalnizca musait arac
        // kilit alir" diye yazmak, sistemi kullanilamaz hale getirirdi:
        // hicbir arac tasima sirasinda koridoru kilitleyemezdi.
        await fabrika.FiloyuHazirlaAsync();
        await KilitleriTemizleAsync();

        var istemci = await fabrika.IstemciAsync();
        var gorevId = await GorevOlusturAsync();
        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        await MesgullestirAsync();

        var yanit = await istemci.PostAsJsonAsync(
            $"/api/resources/{Dock}/lock", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    private async Task DurumAyarlaAsync(bool sarjda = false, int batarya = 100)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var agv = await db.Agvs.SingleAsync(a => a.Id == Agv01);

        if (sarjda)
        {
            agv.SarjaAl();
        }

        agv.BataryaBildir(batarya);
        await db.SaveChangesAsync();
    }

    private async Task MesgullestirAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var agv = await db.Agvs.SingleAsync(a => a.Id == Agv01);
        agv.Mesgullestir();
        await db.SaveChangesAsync();
    }

    private async Task KilitleriTemizleAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<Tasks.Persistence.TasksDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE tasks.resource_lock SET released_at_utc = now() WHERE released_at_utc IS NULL");
    }

    private async Task<Guid> GorevOlusturAsync()
    {
        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            "/api/tasks", new CreateTaskCommand(Kabul, RafA1, "MLZ-UYGUNLUK", 1, 1));

        yanit.EnsureSuccessStatusCode();
        return (await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
    }

    private sealed record OlusturmaYaniti(Guid Id);

    private sealed record HataYaniti(string Code, string Message);
}
