using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FleetOps.IntegrationTests.Altyapi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetOps.IntegrationTests;

// Sayac bellekte tutuldugu icin her senaryo KENDI fabrikasiyla calisiyor;
// paylasilan fabrikada testler birbirinin kilidine takilirdi.
[Collection(VeritabaniKoleksiyonu.Ad)]
public class GirisGuvenligiTests(FleetOpsApiFactory fabrika)
{
    private const int Sinir = 3;

    [Fact]
    public async Task Olmayan_kullanici_var_olan_kadar_surede_reddedilir()
    {
        // Olculdu (2026-09-11): duzeltmeden once 63 ms'ye karsi 1 ms idi.
        // Esik bilerek gevsek: amac mikro-olcum degil, 60 kat farkin
        // kapandigini gormek. Paralel CI makinesinde de tutmali.
        await using var f = Fabrika(sinir: 1000);
        var istemci = f.CreateClient();

        // Isinma: ilk istekler JIT ve baglanti havuzu maliyeti tasir.
        await DeneAsync(istemci, "supervisor", "yanlis");
        await DeneAsync(istemci, "isinma-yok", "yanlis");

        var varOlan = await MedyanAsync(() => DeneAsync(istemci, "supervisor", "yanlis"));
        var olmayan = await MedyanAsync(() => DeneAsync(istemci, $"yok-{Guid.NewGuid():N}", "yanlis"));

        Assert.True(
            olmayan >= varOlan * 0.3,
            $"Sure sizintisi: var olan {varOlan:F1} ms, olmayan {olmayan:F1} ms");
    }

    [Fact]
    public async Task Art_arda_basarisiz_denemeden_sonra_dogru_parola_da_reddedilir()
    {
        await using var f = Fabrika();
        var istemci = f.CreateClient();

        for (var i = 0; i < Sinir; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await DeneAsync(istemci, "operator", "yanlis"));
        }

        // Kilit, dogru parolayi da reddediyor: aksi halde saldirgan kilitten
        // sonra da denemeye devam edip dogru olani 200'den taniyabilirdi.
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            await DeneAsync(istemci, "operator", FleetOpsApiFactory.OperatorParolasi));
    }

    [Fact]
    public async Task Kilit_yalnizca_o_kullaniciyi_etkiler()
    {
        await using var f = Fabrika();
        var istemci = f.CreateClient();

        for (var i = 0; i < Sinir; i++)
        {
            await DeneAsync(istemci, "operator", "yanlis");
        }

        Assert.Equal(
            HttpStatusCode.OK,
            await DeneAsync(istemci, "supervisor", FleetOpsApiFactory.SupervisorParolasi));
    }

    [Fact]
    public async Task Buyuk_kucuk_harf_degistirmek_kilidi_asamaz()
    {
        await using var f = Fabrika();
        var istemci = f.CreateClient();

        for (var i = 0; i < Sinir; i++)
        {
            await DeneAsync(istemci, "operator", "yanlis");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, await DeneAsync(istemci, "OPERATOR", "yanlis"));
    }

    [Fact]
    public async Task Olmayan_kullanici_adi_da_ayni_sekilde_kilitlenir()
    {
        // Yalnizca var olanlar kilitlenseydi 429/401 farki, sure sizintisinin
        // kapattigi bilgiyi baska kapidan verirdi.
        await using var f = Fabrika();
        var istemci = f.CreateClient();

        for (var i = 0; i < Sinir; i++)
        {
            await DeneAsync(istemci, "boyle-biri-yok", "yanlis");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, await DeneAsync(istemci, "boyle-biri-yok", "yanlis"));
    }

    [Fact]
    public async Task Basarili_giris_sayaci_sifirlar()
    {
        // Kontrol testi: sayac hic sifirlanmasaydi, parolasini ara sira
        // yanlis yazan kullanici gun icinde kendini kilitlerdi.
        await using var f = Fabrika();
        var istemci = f.CreateClient();

        for (var i = 0; i < Sinir - 1; i++)
        {
            await DeneAsync(istemci, "operator", "yanlis");
        }

        Assert.Equal(
            HttpStatusCode.OK,
            await DeneAsync(istemci, "operator", FleetOpsApiFactory.OperatorParolasi));

        for (var i = 0; i < Sinir - 1; i++)
        {
            await DeneAsync(istemci, "operator", "yanlis");
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await DeneAsync(istemci, "operator", "yanlis"));
    }

    private WebApplicationFactory<Program> Fabrika(int sinir = Sinir) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:FleetOps", fabrika.BaglantiMetni);
            b.UseSetting("Giris:AzamiBasarisiz", sinir.ToString());
            b.UseSetting("Simulator:Enabled", "false");
            b.UseSetting("Outbox:PollInterval", "01:00:00");
            b.UseSetting("Outbox:CleanupInterval", "01:00:00");
            b.UseSetting("ResourceLock:ReaperInterval", "01:00:00");
            b.UseSetting("Sarj:Interval", "01:00:00");
            b.UseSetting("Jwt:SigningKey", "test-imza-anahtari-en-az-32-bayt-uzunlugunda-olmali");
        });

    private static async Task<HttpStatusCode> DeneAsync(HttpClient istemci, string ad, string parola)
    {
        var yanit = await istemci.PostAsJsonAsync("/api/auth/login", new { userName = ad, password = parola });
        return yanit.StatusCode;
    }

    private static async Task<double> MedyanAsync(Func<Task> islem)
    {
        var sureler = new List<double>();
        for (var i = 0; i < 7; i++)
        {
            var kronometre = Stopwatch.StartNew();
            await islem();
            sureler.Add(kronometre.Elapsed.TotalMilliseconds);
        }

        sureler.Sort();
        return sureler[sureler.Count / 2];
    }
}
