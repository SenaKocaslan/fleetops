using System.Net.Http.Headers;
using FleetOps.IntegrationTests.Altyapi;

namespace FleetOps.IntegrationTests;

// CORS yapilandirmasi sessizce bozulursa bunu ancak tarayicida fark ederiz.
// Burada sunucunun yanit basliklarina bakarak erken yakaliyoruz.
[Collection(VeritabaniKoleksiyonu.Ad)]
public class CorsTests(FleetOpsApiFactory fabrika)
{
    private const string IzinliOrigin = "http://localhost:4200";

    [Fact]
    public async Task Izinli_origin_icin_cors_basligi_doner()
    {
        var istek = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        istek.Headers.Add("Origin", IzinliOrigin);

        var yanit = await fabrika.CreateClient().SendAsync(istek);

        Assert.True(yanit.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(IzinliOrigin, yanit.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Izinsiz_origin_icin_cors_basligi_donmez()
    {
        var istek = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        istek.Headers.Add("Origin", "http://kotu-site.example");

        var yanit = await fabrika.CreateClient().SendAsync(istek);

        Assert.False(yanit.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_istegi_post_metoduna_izin_verir()
    {
        // Tarayici, POST gondermeden once OPTIONS ile izin sorar.
        var istek = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        istek.Headers.Add("Origin", IzinliOrigin);
        istek.Headers.Add("Access-Control-Request-Method", "POST");
        istek.Headers.Add("Access-Control-Request-Headers", "content-type");

        var yanit = await fabrika.CreateClient().SendAsync(istek);

        Assert.True(yanit.IsSuccessStatusCode);
        Assert.Contains(
            "POST",
            yanit.Headers.GetValues("Access-Control-Allow-Methods").SelectMany(v => v.Split(',').Select(x => x.Trim())));
    }
}
