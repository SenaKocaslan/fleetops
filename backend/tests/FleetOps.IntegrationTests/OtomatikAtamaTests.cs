using System.Net;
using System.Net.Http.Json;
using FleetOps.Api.Dispatch;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

// Strateji saf bir fonksiyon: veritabani gerektirmiyor, koleksiyon
// fixture'ina bilerek bagli degil.
public class AtamaStratejisiTests
{
    [Fact]
    public void Bataryasi_en_yuksek_arac_secilir()
    {
        var secilen = new EnYuksekBataryaStratejisi().Sec(
        [
            new AtamaAdayi(Guid.NewGuid(), "AGV-01", 40),
            new AtamaAdayi(Guid.NewGuid(), "AGV-02", 95),
            new AtamaAdayi(Guid.NewGuid(), "AGV-03", 70),
        ]);

        Assert.Equal("AGV-02", secilen?.Code);
    }

    [Fact]
    public void Esit_bataryada_sonuc_her_cagrida_ayni()
    {
        var adaylar = new List<AtamaAdayi>
        {
            new(Guid.NewGuid(), "AGV-09", 80),
            new(Guid.NewGuid(), "AGV-02", 80),
        };
        var strateji = new EnYuksekBataryaStratejisi();

        // Esitlik bozucu olmasaydi sonuc giris sirasina baglanir, kural
        // "ayni girdi ayni cikti" olmaktan cikardi.
        Assert.Equal("AGV-02", strateji.Sec(adaylar)?.Code);
        Assert.Equal("AGV-02", strateji.Sec([.. adaylar.AsEnumerable().Reverse()])?.Code);
    }

    [Fact]
    public void Aday_yoksa_null_doner()
    {
        Assert.Null(new EnYuksekBataryaStratejisi().Sec([]));
    }
}

[Collection(VeritabaniKoleksiyonu.Ad)]
public class OtomatikAtamaTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Agv02 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Agv03 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Kabul = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid RafA1 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    [Fact]
    public async Task Bekleyen_gorevler_musait_araclara_dagitilir()
    {
        await HavuzuHazirlaAsync();
        var yuksek = await GorevOlusturAsync("OTO-YUKSEK", oncelik: 9);
        var dusuk = await GorevOlusturAsync("OTO-DUSUK", oncelik: 1);

        var ozet = await CalistirAsync();

        // Iki musait arac var (AGV-03 sarjda), iki gorev atandi.
        Assert.Equal(2, ozet.Atananlar.Count);
        Assert.Equal(2, ozet.MusaitAgv);

        // Onceligi yuksek gorev once dagitilir, dolayisiyla bataryasi
        // en yuksek araci o alir.
        var ilk = ozet.Atananlar[0];
        Assert.Equal("OTO-YUKSEK", ilk.MaterialCode);
        Assert.Equal("AGV-02", ilk.AgvCode);

        Assert.Contains(ozet.Atananlar, a => a.TaskId == dusuk);
        Assert.Contains(ozet.Atananlar, a => a.TaskId == yuksek);
    }

    [Fact]
    public async Task Ayni_arac_tek_turda_iki_gorev_almaz()
    {
        // Bu testin kalbi su: Fleet aracin mesgullestigini ancak outbox
        // olayi teslim edildikten sonra ogrenir. Tur icinde AGV sorgusu
        // bastan cekilseydi ayni arac hala "musait" gorunurdu.
        await HavuzuHazirlaAsync();
        await SadeceBirAracBirakAsync();

        for (var i = 0; i < 4; i++)
        {
            await GorevOlusturAsync($"OTO-TEK-{i}", oncelik: 5);
        }

        var ozet = await CalistirAsync();

        Assert.Single(ozet.Atananlar);
        Assert.Equal(Agv01, ozet.Atananlar[0].AgvId);
    }

    [Fact]
    public async Task Ikinci_cagri_ayni_araca_ikinci_gorev_vermez()
    {
        // Outbox turu hic calistirilmadan tekrar cagriliyor: Fleet hala
        // "musait" diyor. Ikinci atamayi engelleyen sey veritabanindaki
        // kismi tekil indeks.
        await HavuzuHazirlaAsync();
        await SadeceBirAracBirakAsync();
        await GorevOlusturAsync("OTO-IKI-1", oncelik: 5);
        await GorevOlusturAsync("OTO-IKI-2", oncelik: 5);

        var ilk = await CalistirAsync();
        var ikinci = await CalistirAsync();

        Assert.Single(ilk.Atananlar);
        Assert.Empty(ikinci.Atananlar);

        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        var acik = await db.TransportTasks
            .SelectMany(t => t.Assignments)
            .CountAsync(a => a.AgvId == Agv01 && a.CompletedAtUtc == null);

        Assert.Equal(1, acik);
    }

    [Fact]
    public async Task Bekleyen_olmayan_gorevler_dagitima_girmez()
    {
        // Filtre kalkarsa bu gorev onceligi yuksek oldugu icin listenin
        // basina gecer; atanamaz ama sirayi isgal eder ve asil bekleyen
        // gorev aracsiz kalirdi.
        await HavuzuHazirlaAsync();
        await SadeceBirAracBirakAsync();

        var yurutulen = await GorevOlusturAsync("OTO-YURUTULEN", oncelik: 9);
        var istemci = await fabrika.IstemciAsync();
        await istemci.PostAsJsonAsync($"/api/tasks/{yurutulen}/assign", new { agvId = Agv02 });
        await istemci.PostAsync($"/api/tasks/{yurutulen}/start", null);

        var bekleyen = await GorevOlusturAsync("OTO-BEKLEYEN", oncelik: 1);

        var ozet = await CalistirAsync();

        Assert.Equal(1, ozet.BekleyenGorev);
        var atama = Assert.Single(ozet.Atananlar);
        Assert.Equal(bekleyen, atama.TaskId);
        Assert.Equal(Agv01, atama.AgvId);
    }

    [Fact]
    public async Task Musait_arac_yoksa_hicbir_gorev_atanmaz()
    {
        await HavuzuHazirlaAsync();
        await TumAraclariSarjaAlAsync();
        await GorevOlusturAsync("OTO-BOS", oncelik: 5);

        var ozet = await CalistirAsync();

        Assert.Empty(ozet.Atananlar);
        Assert.Equal(0, ozet.MusaitAgv);
    }

    [Fact]
    public async Task Operator_otomatik_atama_yapamaz()
    {
        // Gorev planlama supervisor yetkisi; operator gorev yurutur, dagitmaz.
        var istemci = await fabrika.IstemciAsync(
            FleetOpsApiFactory.OperatorAdi, FleetOpsApiFactory.OperatorParolasi);

        var yanit = await istemci.PostAsync("/api/dispatch/auto-assign", null);

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    private async Task<OtomatikAtamaOzeti> CalistirAsync()
    {
        var yanit = await (await fabrika.IstemciAsync()).PostAsync("/api/dispatch/auto-assign", null);
        yanit.EnsureSuccessStatusCode();

        return (await yanit.Content.ReadFromJsonAsync<OtomatikAtamaOzeti>())!;
    }

    // Havuzda baska testlerden kalan bekleyen gorev olursa dagitim onlari
    // alir ve bu testlerin sayilari tutmaz.
    private async Task HavuzuHazirlaAsync()
    {
        await fabrika.AtamalariKapatAsync();

        using (var kapsam = fabrika.KapsamAc())
        {
            var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
            await db.TransportTasks.ExecuteDeleteAsync();
        }

        using var fleetKapsam = fabrika.KapsamAc();
        var fleet = fleetKapsam.ServiceProvider.GetRequiredService<FleetDbContext>();

        // Baska testler filoya AGV ekliyor. Hepsini servis disi birakip
        // yalnizca tohum araclari sahneye aliyoruz; aksi halde musait arac
        // sayisi testin disinda degisiyor.
        foreach (var agv in await fleet.Agvs.ToListAsync())
        {
            agv.ServisDisiBirak();
        }

        var birinci = await fleet.Agvs.SingleAsync(a => a.Id == Agv01);
        birinci.ServiseAl();
        birinci.BataryaBildir(80);

        var ikinci = await fleet.Agvs.SingleAsync(a => a.Id == Agv02);
        ikinci.ServiseAl();
        ikinci.BataryaBildir(95);

        // AGV-03 sarjda: musait arac sayisinin ikide kalmasi bilincli.
        var ucuncu = await fleet.Agvs.SingleAsync(a => a.Id == Agv03);
        ucuncu.SarjaAl();

        await fleet.SaveChangesAsync();
    }

    private async Task SadeceBirAracBirakAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var fleet = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();

        foreach (var agv in await fleet.Agvs.Where(a => a.Id != Agv01).ToListAsync())
        {
            agv.SarjaAl();
        }

        await fleet.SaveChangesAsync();
    }

    private async Task TumAraclariSarjaAlAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var fleet = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();

        foreach (var agv in await fleet.Agvs.ToListAsync())
        {
            agv.SarjaAl();
        }

        await fleet.SaveChangesAsync();
    }

    private async Task<Guid> GorevOlusturAsync(string malzeme, int oncelik)
    {
        var yanit = await (await fabrika.IstemciAsync()).PostAsJsonAsync(
            "/api/tasks", new CreateTaskCommand(Kabul, RafA1, malzeme, 1, oncelik));

        yanit.EnsureSuccessStatusCode();
        return (await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
    }

    private sealed record OlusturmaYaniti(Guid Id);
}
