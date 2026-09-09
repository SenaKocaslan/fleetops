namespace FleetOps.Api.Dispatch;

// Stratejiye Fleet'in kendi tipi degil, notr bir kayit geliyor. Boylece
// atama kurali Fleet modulunun ic yapisina bagli olmuyor ve saf bir
// fonksiyon olarak test edilebiliyor.
public sealed record AtamaAdayi(Guid AgvId, string Code, int BatteryLevel);

// "Atama kurali degisebilir olsun." Kural degistiginde yalnizca bu arayuzun
// yeni bir uygulamasi yazilir; cagiran taraf hic degismez.
public interface IAtamaStratejisi
{
    string Ad { get; }

    AtamaAdayi? Sec(IReadOnlyList<AtamaAdayi> adaylar);
}

// Varsayilan kural: bataryasi en yuksek arac. Gerekce, gorevin yarida
// kalma ihtimalini dusurmek -- gorev suresi bilinmedigi icin elde tek
// olcut batarya. Esitlikte kod'a gore sabit bir sira: aksi halde ayni
// girdi icin farkli sonuc donebilir ve test kararsiz olur.
public sealed class EnYuksekBataryaStratejisi : IAtamaStratejisi
{
    public string Ad => "EnYuksekBatarya";

    public AtamaAdayi? Sec(IReadOnlyList<AtamaAdayi> adaylar) => adaylar
        .OrderByDescending(a => a.BatteryLevel)
        .ThenBy(a => a.Code, StringComparer.Ordinal)
        .FirstOrDefault();
}
