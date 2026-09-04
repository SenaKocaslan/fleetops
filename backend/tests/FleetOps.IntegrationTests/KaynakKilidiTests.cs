using System.Net;
using System.Net.Http.Json;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Infrastructure;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class KaynakKilidiTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Agv02 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid Dock = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Koridor = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Asansor = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    [Fact]
    public async Task Tohumlanan_kaynaklar_listelenir()
    {
        var kaynaklar = await (await fabrika.IstemciAsync())
            .GetFromJsonAsync<List<ResourceSummary>>("/api/resources");

        var kodlar = kaynaklar!.Select(k => k.Code).ToList();
        Assert.Contains("DOCK-1", kodlar);
        Assert.Contains("CORRIDOR-A", kodlar);
        Assert.Contains("LIFT-1", kodlar);
    }

    [Fact]
    public async Task Kilit_alinir_ve_listede_tutan_agv_gorunur()
    {
        var istemci = await fabrika.IstemciAsync();
        await KilitleriTemizleAsync(Koridor);

        var yanit = await istemci.PostAsJsonAsync(
            $"/api/resources/{Koridor}/lock", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var kaynaklar = await istemci.GetFromJsonAsync<List<ResourceSummary>>("/api/resources");
        var koridor = Assert.Single(kaynaklar!, k => k.Id == Koridor);

        Assert.Equal(Agv01, koridor.LockedByAgvId);
        Assert.NotNull(koridor.LockExpiresAtUtc);
    }

    [Fact]
    public async Task Kilitli_kaynak_ikinci_agv_tarafindan_alinamaz()
    {
        var istemci = await fabrika.IstemciAsync();
        await KilitleriTemizleAsync(Asansor);

        await istemci.PostAsJsonAsync($"/api/resources/{Asansor}/lock", new { agvId = Agv01 });
        var ikinci = await istemci.PostAsJsonAsync(
            $"/api/resources/{Asansor}/lock", new { agvId = Agv02 });

        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
        var hata = await ikinci.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Resource.KaynakMesgul", hata?.Code);
    }

    [Fact]
    public async Task Paralel_kilit_isteklerinden_yalnizca_biri_basarili_olur()
    {
        const int istekSayisi = 8;
        var istemci = await fabrika.IstemciAsync();
        await KilitleriTemizleAsync(Dock);

        // Istekleri sirayla baslatirsak yaris hic olusmaz; hepsi tek kapidan.
        var kapi = new TaskCompletionSource();
        var istekler = Enumerable.Range(0, istekSayisi).Select(i => Task.Run(async () =>
        {
            await kapi.Task;
            return await istemci.PostAsJsonAsync(
                $"/api/resources/{Dock}/lock", new { agvId = Guid.NewGuid() });
        })).ToArray();

        kapi.SetResult();
        var yanitlar = await Task.WhenAll(istekler);
        var durumlar = yanitlar.Select(y => y.StatusCode).ToList();

        Assert.Single(durumlar, d => d == HttpStatusCode.OK);
        Assert.All(
            durumlar.Where(d => d != HttpStatusCode.OK),
            d => Assert.Equal(HttpStatusCode.Conflict, d));

        Assert.Equal(1, await AktifKilitSayisiAsync(Dock));
    }

    [Fact]
    public async Task Kilidi_tutmayan_agv_birakamaz()
    {
        var istemci = await fabrika.IstemciAsync();
        await KilitleriTemizleAsync(Koridor);
        await istemci.PostAsJsonAsync($"/api/resources/{Koridor}/lock", new { agvId = Agv01 });

        var yanit = await istemci.PostAsJsonAsync(
            $"/api/resources/{Koridor}/release", new { agvId = Agv02 });

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
        var hata = await yanit.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Resource.KilidiBaskasiTutuyor", hata?.Code);
    }

    [Fact]
    public async Task Birakilan_kaynak_tekrar_kilitlenebilir()
    {
        var istemci = await fabrika.IstemciAsync();
        await KilitleriTemizleAsync(Asansor);

        await istemci.PostAsJsonAsync($"/api/resources/{Asansor}/lock", new { agvId = Agv01 });
        var birak = await istemci.PostAsJsonAsync(
            $"/api/resources/{Asansor}/release", new { agvId = Agv01 });
        var tekrar = await istemci.PostAsJsonAsync(
            $"/api/resources/{Asansor}/lock", new { agvId = Agv02 });

        Assert.Equal(HttpStatusCode.NoContent, birak.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tekrar.StatusCode);

        Assert.Equal(2, await KilitSayisiAsync(Asansor));
        Assert.Equal(1, await AktifKilitSayisiAsync(Asansor));
    }

    [Fact]
    public async Task Aktif_kilidi_olmayan_kaynak_birakilamaz()
    {
        await KilitleriTemizleAsync(Koridor);

        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/resources/{Koridor}/release", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
    }

    [Fact]
    public async Task Olmayan_kaynak_kilitlenemez()
    {
        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/resources/{Guid.NewGuid()}/lock", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
    }

    [Fact]
    public async Task Reaper_suresi_dolan_kilidi_serbest_birakir()
    {
        await KilitleriTemizleAsync(Dock);
        await SuresiDolmusKilitEkleAsync(Dock, Agv01);

        Assert.Equal(1, await AktifKilitSayisiAsync(Dock));

        var birakilan = await ReaperCalistirAsync();

        Assert.True(birakilan >= 1);
        Assert.Equal(0, await AktifKilitSayisiAsync(Dock));

        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/resources/{Dock}/lock", new { agvId = Agv02 });
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Fact]
    public async Task Reaper_suresi_dolmamis_kilide_dokunmaz()
    {
        var istemci = await fabrika.IstemciAsync();
        await KilitleriTemizleAsync(Koridor);
        await istemci.PostAsJsonAsync($"/api/resources/{Koridor}/lock", new { agvId = Agv01 });

        await ReaperCalistirAsync();

        Assert.Equal(1, await AktifKilitSayisiAsync(Koridor));
    }

    [Fact]
    public void Reaper_barindirilan_servis_olarak_kayitli()
    {
        var servisler = fabrika.Services.GetServices<IHostedService>();

        Assert.Single(servisler.OfType<LockReaper>());
    }

    private async Task<int> ReaperCalistirAsync()
    {
        var reaper = fabrika.Services.GetServices<IHostedService>().OfType<LockReaper>().Single();
        return await reaper.BirTurCalistirAsync(CancellationToken.None);
    }

    private async Task KilitleriTemizleAsync(Guid kaynakId)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.ResourceLocks.Where(l => l.ResourceId == kaynakId).ExecuteDeleteAsync();
    }

    private async Task SuresiDolmusKilitEkleAsync(Guid kaynakId, Guid agvId)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        var kilit = ResourceLock.Acquire(
            Guid.NewGuid(),
            kaynakId,
            agvId,
            DateTime.UtcNow.AddMinutes(-10),
            TimeSpan.FromMinutes(1)).Value;

        db.ResourceLocks.Add(kilit);
        await db.SaveChangesAsync();
    }

    private async Task<int> AktifKilitSayisiAsync(Guid kaynakId)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        return await db.ResourceLocks
            .CountAsync(l => l.ResourceId == kaynakId && l.ReleasedAtUtc == null);
    }

    private async Task<int> KilitSayisiAsync(Guid kaynakId)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        return await db.ResourceLocks.CountAsync(l => l.ResourceId == kaynakId);
    }

    private sealed record HataYaniti(string Code, string Message);
}
