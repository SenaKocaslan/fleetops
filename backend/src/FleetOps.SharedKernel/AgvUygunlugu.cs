namespace FleetOps.SharedKernel;

public sealed record AgvUygunlugu(
    bool Bulundu,
    string Kod,
    bool GorevAlabilir,
    bool SahadaCalisabilir)
{
    public static readonly AgvUygunlugu Yok = new(false, string.Empty, false, false);
}

// MODULLER ARASI SENKRON SORGU. Integration event'ten farki sudur: bu sorunun
// cevabi KARAR ANINDA lazim, "birazdan ogrensen de olur" degil. Olay uzerinden
// beslenen bir kopya tablo, dogasi geregi bir tik geride olur ve tam da
// karar aninda yanlis cevap verir.
//
// Kural hala korunuyor: Tasks, Fleet'i referans vermiyor ve Fleet'in
// DbContext'ini, tiplerini, handler'larini gormuyor. Gordugu tek sey bu
// arayuz ve dondurdugu notr kayit.
public interface IAgvUygunlukSaglayici
{
    Task<AgvUygunlugu> GetirAsync(Guid agvId, CancellationToken cancellationToken);
}
