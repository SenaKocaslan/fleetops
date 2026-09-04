using System.Net.Http.Json;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class SayfalamaTests(FleetOpsApiFactory fabrika)
{
    [Fact]
    public async Task Varsayilan_sayfa_boyutu_uygulanir()
    {
        var istemci = await fabrika.IstemciAsync();
        await EnAzGorevOlusturAsync(istemci, PageRequest.VarsayilanBoyut + 5);

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>("/api/tasks");

        Assert.Equal(1, sayfa!.Page);
        Assert.Equal(PageRequest.VarsayilanBoyut, sayfa.PageSize);
        Assert.Equal(PageRequest.VarsayilanBoyut, sayfa.Items.Count);
        Assert.True(sayfa.TotalCount >= PageRequest.VarsayilanBoyut + 5);
        Assert.True(sayfa.HasNext);
    }

    [Fact]
    public async Task Ikinci_sayfa_birinciyle_ayni_kayitlari_icermez()
    {
        var istemci = await fabrika.IstemciAsync();
        await EnAzGorevOlusturAsync(istemci, 25);

        var birinci = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?page=1&pageSize=10");
        var ikinci = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?page=2&pageSize=10");

        var birinciIdler = birinci!.Items.Select(g => g.Id).ToHashSet();
        var ikinciIdler = ikinci!.Items.Select(g => g.Id).ToHashSet();

        Assert.Equal(10, birinciIdler.Count);
        Assert.Equal(10, ikinciIdler.Count);
        Assert.Empty(birinciIdler.Intersect(ikinciIdler));
    }

    [Fact]
    public async Task Ayni_sayfa_iki_kez_istenince_ayni_kayitlari_doner()
    {
        // Siralamada esitlik bozucu yoksa PostgreSQL ayni sorguya farkli sira
        // dondurebiliyor; bu test o durumu yakalar.
        var istemci = await fabrika.IstemciAsync();
        await EnAzGorevOlusturAsync(istemci, 25);

        var birinci = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?page=2&pageSize=10");
        var ikinci = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?page=2&pageSize=10");

        Assert.Equal(
            birinci!.Items.Select(g => g.Id),
            ikinci!.Items.Select(g => g.Id));
    }

    [Fact]
    public async Task Asiri_sayfa_boyutu_ustten_sinirlanir()
    {
        var istemci = await fabrika.IstemciAsync();

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?pageSize=1000000");

        Assert.Equal(PageRequest.AzamiBoyut, sayfa!.PageSize);
        Assert.True(sayfa.Items.Count <= PageRequest.AzamiBoyut);
    }

    // Anlamsiz ama gecerli sayi: PageRequest sessizce ilk sayfaya cekiyor.
    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-3")]
    public async Task Anlamsiz_sayfa_numarasi_ilk_sayfaya_duser(string sorgu)
    {
        var istemci = await fabrika.IstemciAsync();

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks" + sorgu);

        Assert.Equal(1, sayfa!.Page);
    }

    // Sayi bile olmayan deger: Minimal API baglama sirasinda, benim kodum
    // calismadan reddediyor. Bicimsel hata istemcinin hatasidir; sessizce
    // duzeltmek yanlis olurdu.
    [Theory]
    [InlineData("?page=abc")]
    [InlineData("?pageSize=cok")]
    public async Task Sayi_olmayan_sayfa_parametresi_400_doner(string sorgu)
    {
        var istemci = await fabrika.IstemciAsync();

        var yanit = await istemci.GetAsync("/api/tasks" + sorgu);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, yanit.StatusCode);
    }

    [Fact]
    public async Task Var_olmayan_sayfa_bos_liste_ama_toplami_dogru_doner()
    {
        var istemci = await fabrika.IstemciAsync();

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?page=99999&pageSize=10");

        Assert.Empty(sayfa!.Items);
        Assert.True(sayfa.TotalCount > 0);
        Assert.False(sayfa.HasNext);
    }

    [Fact]
    public async Task Malzeme_kodu_aramasi_sonucu_daraltir()
    {
        var istemci = await fabrika.IstemciAsync();
        var malzeme = $"ARA-{Guid.NewGuid():N}"[..12];

        await istemci.PostAsJsonAsync("/api/tasks", new
        {
            fromLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            toLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
            materialCode = malzeme,
            quantity = 1,
            priority = 1,
        });

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            $"/api/tasks?materialCode={malzeme}");

        Assert.Single(sayfa!.Items);
        Assert.Equal(malzeme, sayfa.Items[0].MaterialCode);

        // Toplam sayi filtreden SONRA hesaplanmali; yoksa "1 kayit gosteriliyor
        // ama 90 kayit var" gibi tutarsiz bir sayfalayici cikar.
        Assert.Equal(1, sayfa.TotalCount);
        Assert.False(sayfa.HasNext);
    }

    [Fact]
    public async Task Arama_buyuk_kucuk_harf_ayirmaz()
    {
        var istemci = await fabrika.IstemciAsync();
        var malzeme = $"CaSe-{Guid.NewGuid():N}"[..12];

        await istemci.PostAsJsonAsync("/api/tasks", new
        {
            fromLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            toLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
            materialCode = malzeme,
            quantity = 1,
            priority = 1,
        });

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            $"/api/tasks?materialCode={malzeme.ToUpperInvariant()}");

        Assert.Single(sayfa!.Items);
    }

    [Fact]
    public async Task Eslesmeyen_arama_bos_sayfa_doner()
    {
        var istemci = await fabrika.IstemciAsync();

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?materialCode=boyle-bir-kod-yok");

        Assert.Empty(sayfa!.Items);
        Assert.Equal(0, sayfa.TotalCount);
        Assert.Equal(0, sayfa.TotalPages);
    }

    private static async Task EnAzGorevOlusturAsync(HttpClient istemci, int adet)
    {
        var mevcut = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>("/api/tasks");

        for (var i = mevcut!.TotalCount; i < adet; i++)
        {
            await istemci.PostAsJsonAsync("/api/tasks", new
            {
                fromLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
                toLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
                materialCode = $"SYF-{i}",
                quantity = 1,
                priority = 5,
            });
        }
    }
}
