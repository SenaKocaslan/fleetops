using FleetOps.SharedKernel.Domain;

namespace FleetOps.Stock.Domain;

public static class StockErrors
{
    public static readonly Error LokasyonKoduBos =
        new("Stock.LokasyonKoduBos", "Lokasyon kodu bos olamaz.");

    public static readonly Error MalzemeKoduBos =
        new("Stock.MalzemeKoduBos", "Malzeme kodu bos olamaz.");

    public static readonly Error MiktarPozitifOlmali =
        new("Stock.MiktarPozitifOlmali", "Miktar sifirdan buyuk olmalidir.");

    public static readonly Error AyniLokasyon =
        new("Stock.AyniLokasyon", "Kaynak ve hedef lokasyon ayni olamaz.");
}
