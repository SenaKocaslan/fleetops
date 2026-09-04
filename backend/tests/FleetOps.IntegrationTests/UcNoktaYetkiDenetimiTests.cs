using FleetOps.IntegrationTests.Altyapi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

// Yeni bir uc nokta eklenirken RequireAuthorization unutmak sessiz bir
// guvenlik acigi: hicbir test kirilmaz, kimse fark etmez. Bu test tum
// uc noktalari sayip denetliyor.
[Collection(VeritabaniKoleksiyonu.Ad)]
public class UcNoktaYetkiDenetimiTests(FleetOpsApiFactory fabrika)
{
    // Bilerek anonim olanlar. Listeye ekleme yapmak bilincli bir karar olmali.
    private static readonly HashSet<string> AnonimOlmasiBeklenenler = new()
    {
        "/api/auth/login",
    };

    [Fact]
    public void Tum_api_uc_noktalari_yetkilendirme_ister()
    {
        var korumasizlar = ApiUcNoktalari()
            .Where(u => !AnonimOlmasiBeklenenler.Contains(u.Desen))
            .Where(u => !u.YetkiVar)
            .Select(u => u.Desen)
            .ToList();

        Assert.True(
            korumasizlar.Count == 0,
            "RequireAuthorization eksik: " + string.Join(", ", korumasizlar));
    }

    [Fact]
    public void Anonim_kalmasi_beklenen_uc_noktalar_gercekten_var()
    {
        // Liste eskirse (uc nokta yeniden adlandirilirsa) bu test uyarir;
        // aksi halde beyaz liste sessizce anlamsizlasir.
        var desenler = ApiUcNoktalari().Select(u => u.Desen).ToHashSet();

        foreach (var beklenen in AnonimOlmasiBeklenenler)
        {
            Assert.Contains(beklenen, desenler);
        }
    }

    [Fact]
    public void Signalr_hublari_da_yetkilendirme_ister()
    {
        var hublar = TumUcNoktalar()
            .Where(u => u.Desen.StartsWith("/hubs/", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(hublar);
        Assert.All(hublar, u => Assert.True(u.YetkiVar, $"Hub korumasiz: {u.Desen}"));
    }

    private IEnumerable<(string Desen, bool YetkiVar)> ApiUcNoktalari() =>
        TumUcNoktalar().Where(u => u.Desen.StartsWith("/api/", StringComparison.Ordinal));

    private IEnumerable<(string Desen, bool YetkiVar)> TumUcNoktalar() =>
        fabrika.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(u => (
                Desen: u.RoutePattern.RawText ?? string.Empty,
                YetkiVar: u.Metadata.GetMetadata<IAuthorizeData>() is not null))
            .Distinct();
}
