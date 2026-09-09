using System.Net;
using System.Net.Http.Json;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class AtamaEszamanlilikTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Agv02 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Ayni_goreve_iki_baglam_yazarsa_ikincisi_reddedilir()
    {
        await fabrika.AtamalariKapatAsync();
        var gorevId = await GorevOlusturAsync();

        using var kapsam1 = fabrika.KapsamAc();
        using var kapsam2 = fabrika.KapsamAc();
        var db1 = kapsam1.ServiceProvider.GetRequiredService<TasksDbContext>();
        var db2 = kapsam2.ServiceProvider.GetRequiredService<TasksDbContext>();

        var gorev1 = await GorevYukleAsync(db1, gorevId);
        var gorev2 = await GorevYukleAsync(db2, gorevId);
        Assert.Equal(gorev1.Version, gorev2.Version);

        Assert.True(gorev1.Assign(Agv01, DateTime.UtcNow).IsSuccess);
        Assert.True(gorev2.Assign(Agv02, DateTime.UtcNow).IsSuccess);

        await db1.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());

        Assert.Equal(Agv01, await AktifAgvAsync(gorevId));
    }

    [Fact]
    public async Task Paralel_atama_isteklerinden_yalnizca_biri_basarili_olur()
    {
        await fabrika.AtamalariKapatAsync();
        const int istekSayisi = 8;
        var gorevId = await GorevOlusturAsync();
        var istemci = await fabrika.IstemciAsync();

        // Istekleri sirayla baslatirsak yaris hic olusmaz; hepsi tek kapidan.
        // Kaybedenin 409 mu 400 mu aldigi zamanlamaya bagli, ona gore assert
        // yazmak flaky olur.
        var kapi = new TaskCompletionSource();
        var istekler = Enumerable.Range(0, istekSayisi).Select(_ => Task.Run(async () =>
        {
            await kapi.Task;
            return await istemci.PostAsJsonAsync(
                $"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        })).ToArray();

        kapi.SetResult();
        var yanitlar = await Task.WhenAll(istekler);

        var durumlar = yanitlar.Select(y => y.StatusCode).ToList();

        Assert.Single(durumlar, d => d == HttpStatusCode.NoContent);
        Assert.All(
            durumlar.Where(d => d != HttpStatusCode.NoContent),
            d => Assert.Contains(d, new[] { HttpStatusCode.Conflict, HttpStatusCode.BadRequest }));

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        var aktifSayisi = await db.TransportTasks
            .Where(t => t.Id == gorevId)
            .SelectMany(t => t.Assignments)
            .CountAsync(a => a.CompletedAtUtc == null);

        Assert.Equal(1, aktifSayisi);
    }

    [Fact]
    public async Task Atanan_gorev_listede_agv_ile_gorunur()
    {
        await fabrika.AtamalariKapatAsync();
        var gorevId = await GorevOlusturAsync();
        var istemci = await fabrika.IstemciAsync();

        var yanit = await istemci.PostAsJsonAsync(
            $"/api/tasks/{gorevId}/assign", new { agvId = Agv02 });

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?pageSize=100");
        var liste = sayfa!.Items;
        var gorev = Assert.Single(liste, g => g.Id == gorevId);

        Assert.Equal("Assigned", gorev.Status);
        Assert.Equal(Agv02, gorev.AssignedAgvId);
    }

    [Fact]
    public async Task Zaten_atanmis_gorev_ikinci_kez_atanamaz()
    {
        await fabrika.AtamalariKapatAsync();
        var gorevId = await GorevOlusturAsync();
        var istemci = await fabrika.IstemciAsync();

        await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });
        var ikinci = await istemci.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv02 });

        Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
        var hata = await ikinci.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Task.GecersizGecis", hata?.Code);
    }

    [Fact]
    public async Task Olmayan_gorev_icin_404_doner()
    {
        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            $"/api/tasks/{Guid.NewGuid()}/assign", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        var hata = await yanit.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Task.Bulunamadi", hata?.Code);
    }

    [Fact]
    public async Task Acik_gorevi_olan_agv_ikinci_goreve_atanamaz()
    {
        await fabrika.AtamalariKapatAsync();
        var istemci = await fabrika.IstemciAsync();
        var ilk = await GorevOlusturAsync();
        var ikinci = await GorevOlusturAsync();

        var basarili = await istemci.PostAsJsonAsync($"/api/tasks/{ilk}/assign", new { agvId = Agv01 });
        Assert.Equal(HttpStatusCode.NoContent, basarili.StatusCode);

        var reddedilen = await istemci.PostAsJsonAsync($"/api/tasks/{ikinci}/assign", new { agvId = Agv01 });

        // Istemci yanlis bir sey yapmadi, arac mesgul: 409.
        Assert.Equal(HttpStatusCode.Conflict, reddedilen.StatusCode);
        var hata = await reddedilen.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Task.AgvMesgul", hata?.Code);
    }

    [Fact]
    public async Task Gorev_kapaninca_agv_yeniden_atanabilir()
    {
        // Kontrol testi: yukaridaki kural "AGV bir kere atandi mi bir daha
        // atanamaz" seklinde yanlis uygulansaydi da o test yesil yanardi.
        await fabrika.AtamalariKapatAsync();
        var istemci = await fabrika.IstemciAsync();
        var ilk = await GorevOlusturAsync();
        var ikinci = await GorevOlusturAsync();

        await istemci.PostAsJsonAsync($"/api/tasks/{ilk}/assign", new { agvId = Agv01 });
        await istemci.PostAsync($"/api/tasks/{ilk}/start", null);
        await istemci.PostAsync($"/api/tasks/{ilk}/complete", null);

        var yanit = await istemci.PostAsJsonAsync($"/api/tasks/{ikinci}/assign", new { agvId = Agv01 });

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);
    }

    [Fact]
    public async Task Ayni_agv_iki_baglamdan_atanirsa_veritabani_ikincisini_reddeder()
    {
        // HTTP uzerinden paralel istek atmak burada YETMIYOR: olculdu, istekler
        // pratikte sirayla islendigi icin nazik kontrol hepsini yakaliyor ve
        // indeks kaldirilsa bile test yesil yaniyordu. Yarisi deterministik
        // uretmek icin iki ayri baglam kullaniliyor; ikisi de handler'in
        // kontrolunu hic calistirmadan dogrudan yaziyor.
        await fabrika.AtamalariKapatAsync();
        var ilk = await GorevOlusturAsync();
        var ikinci = await GorevOlusturAsync();

        using var kapsam1 = fabrika.KapsamAc();
        using var kapsam2 = fabrika.KapsamAc();
        var db1 = kapsam1.ServiceProvider.GetRequiredService<TasksDbContext>();
        var db2 = kapsam2.ServiceProvider.GetRequiredService<TasksDbContext>();

        var gorev1 = await GorevYukleAsync(db1, ilk);
        var gorev2 = await GorevYukleAsync(db2, ikinci);

        // Iki FARKLI gorev, ayni AGV. Gorevler birbirinin atamalarini
        // gormedigi icin ikisi de kendince gecerli.
        Assert.True(gorev1.Assign(Agv01, DateTime.UtcNow).IsSuccess);
        Assert.True(gorev2.Assign(Agv01, DateTime.UtcNow).IsSuccess);

        await db1.SaveChangesAsync();

        var hata = await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());

        // Herhangi bir hata degil, TAM OLARAK tekil kisit ihlali bekleniyor.
        var kok = Assert.IsType<PostgresException>(hata.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, kok.SqlState);

        using var kontrol = fabrika.KapsamAc();
        var db3 = kontrol.ServiceProvider.GetRequiredService<TasksDbContext>();
        var acik = await db3.TransportTasks
            .SelectMany(t => t.Assignments)
            .CountAsync(a => a.AgvId == Agv01 && a.CompletedAtUtc == null);

        Assert.Equal(1, acik);
    }

    private async Task<Guid> GorevOlusturAsync()
    {
        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskCommand(Guid.NewGuid(), Guid.NewGuid(), "MLZ-ATAMA", 3, 1));

        yanit.EnsureSuccessStatusCode();
        var govde = await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>();
        return govde!.Id;
    }

    private static Task<Tasks.Domain.TransportTask> GorevYukleAsync(TasksDbContext db, Guid id) =>
        db.TransportTasks.Include(t => t.Assignments).SingleAsync(t => t.Id == id);

    private async Task<Guid?> AktifAgvAsync(Guid gorevId)
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        return await db.TransportTasks
            .Where(t => t.Id == gorevId)
            .SelectMany(t => t.Assignments)
            .Where(a => a.CompletedAtUtc == null)
            .Select(a => (Guid?)a.AgvId)
            .SingleOrDefaultAsync();
    }

    private sealed record OlusturmaYaniti(Guid Id);

    private sealed record HataYaniti(string Code, string Message);
}
