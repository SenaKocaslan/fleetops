using FleetOps.Api.Auth;

namespace FleetOps.IntegrationTests;

// Saf birim testi ama FleetOps.UnitTests yalnizca *.Domain projelerini goruyor
// (bilincli kisit) ve hashleyici composition root'ta. Koleksiyon fixture'i
// kullanmadigi icin veritabani ayaga kalkmiyor.
public class ParolaHashleyiciTests
{
    [Fact]
    public void Dogru_parola_dogrulanir()
    {
        var hash = ParolaHashleyici.Hashle("Operator123!");

        Assert.True(ParolaHashleyici.Dogrula("Operator123!", hash));
    }

    [Fact]
    public void Yanlis_parola_reddedilir()
    {
        var hash = ParolaHashleyici.Hashle("Operator123!");

        Assert.False(ParolaHashleyici.Dogrula("operator123!", hash));
    }

    [Fact]
    public void Ayni_parola_iki_kez_hashlenince_farkli_cikti_verir()
    {
        // Tuz her seferinde yeniden uretiliyor. Ayni cikarsa iki kullanicinin
        // ayni parolayi kullandigi tablodan okunabilir hale gelirdi.
        Assert.NotEqual(ParolaHashleyici.Hashle("ayni"), ParolaHashleyici.Hashle("ayni"));
    }

    [Fact]
    public void Hash_duz_parolayi_icermez()
    {
        Assert.DoesNotContain("Operator123!", ParolaHashleyici.Hashle("Operator123!"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("bozuk")]
    [InlineData("100000.gecersiz-base64!.abc")]
    [InlineData("abc.ZGVm.Z2hp")]
    public void Bozuk_hash_kaydi_istisna_firlatmadan_reddedilir(string saklanan)
    {
        Assert.False(ParolaHashleyici.Dogrula("herhangi", saklanan));
    }

    [Fact]
    public void Tohum_kullanici_parolalari_migrationdaki_hashlerle_dogrulaniyor()
    {
        // Bu hash'ler migration'a gomulu. Hashleyici degisirse tohum
        // kullanicilar sessizce giris yapamaz hale gelirdi.
        const string operatorHash =
            "100000.u1dXkDE+WT5SEH2bOdEpZg==.fLwykOL+TgFQ7akkw/MlEPmcBiWkUBi/kxrCkh2WUg8=";
        const string supervisorHash =
            "100000.M5ugf+wFXLnNWojnB4Ok/Q==.dqXIJoJIK5JlCvxEKScj6oTIEkDUkArvkzqkICwGZAQ=";

        Assert.True(ParolaHashleyici.Dogrula("Operator123!", operatorHash));
        Assert.True(ParolaHashleyici.Dogrula("Supervisor123!", supervisorHash));
    }
}
