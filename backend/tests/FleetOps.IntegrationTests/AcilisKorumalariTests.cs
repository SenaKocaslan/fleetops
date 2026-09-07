using FleetOps.Api.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

// Veritabani gerektirmez; koleksiyon fixture'ina bilerek bagli degil.
public class AcilisKorumalariTests
{
    [Theory]
    [InlineData("")]
    [InlineData("kisa-anahtar")]
    public void Yetersiz_imza_anahtari_acilista_hata_verir(string anahtar)
    {
        var hata = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddFleetOpsAuth(Ayar(anahtar)));

        Assert.Contains("Jwt:SigningKey", hata.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Yeterli_uzunluktaki_anahtar_kabul_edilir()
    {
        // Kontrol testi: yukaridaki iki test, kurulum her durumda patladigi
        // icin de yesil yanabilirdi.
        new ServiceCollection().AddFleetOpsAuth(
            Ayar("test-imza-anahtari-en-az-32-bayt-uzunlugunda-olmali"));
    }

    private static IConfiguration Ayar(string anahtar) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:FleetOps"] = "Host=yok;Database=yok;Username=yok;Password=yok",
            ["Jwt:SigningKey"] = anahtar,
        }).Build();
}
