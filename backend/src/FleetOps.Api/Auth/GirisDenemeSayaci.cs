using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FleetOps.Api.Auth;

public sealed class GirisOptions
{
    public const string Bolum = "Giris";

    public int AzamiBasarisiz { get; set; } = 5;

    public TimeSpan Pencere { get; set; } = TimeSpan.FromMinutes(5);
}

// Kullanici adi basina basarisiz giris sayaci. IP bazli sinir burada DEGIL,
// nginx'te: API nginx'in arkasinda oldugu icin buradan bakildiginda her istek
// ayni IP'den gelir ve tum kullanicilar tek bir kovayi paylasirdi.
//
// SAYAC OLMAYAN KULLANICI ADLARI ICIN DE TUTULUYOR. Yalnizca var olanlar
// kilitlenseydi, "429 mu 401 mi" farki hangi kullanici adlarinin var
// oldugunu sizdirirdi -- sure sizintisini kapatip ayni bilgiyi baska
// kapidan vermek olurdu.
//
// Bilinen bedel: saldirgan, baskasinin kullanici adina yanlis parola
// deneyerek onu Pencere suresince kilitleyebilir. Pencere kisa tutuluyor.
// Tek surecte tutuluyor; API birden fazla kopya calisirsa her kopyanin
// sayaci ayridir.
public sealed class GirisDenemeSayaci(IMemoryCache onbellek, IOptions<GirisOptions> ayarlar)
{
    private readonly GirisOptions _ayarlar = ayarlar.Value;

    public bool KilitliMi(string kullaniciAdi) =>
        onbellek.TryGetValue(Anahtar(kullaniciAdi), out Sayac? sayac)
        && sayac!.Deger >= _ayarlar.AzamiBasarisiz;

    public void BasarisizKaydet(string kullaniciAdi)
    {
        var sayac = onbellek.GetOrCreate(Anahtar(kullaniciAdi), giris =>
        {
            giris.AbsoluteExpirationRelativeToNow = _ayarlar.Pencere;
            return new Sayac();
        })!;

        sayac.Artir();
    }

    public void Sifirla(string kullaniciAdi) => onbellek.Remove(Anahtar(kullaniciAdi));

    // "Supervisor" ile "supervisor" ayni kovaya dusmeli; aksi halde buyuk-
    // kucuk harf degistirerek sinir asilabilirdi.
    private static string Anahtar(string kullaniciAdi) =>
        "giris:" + kullaniciAdi.Trim().ToLowerInvariant();

    private sealed class Sayac
    {
        private int _deger;

        public int Deger => Volatile.Read(ref _deger);

        public void Artir() => Interlocked.Increment(ref _deger);
    }
}
