using FleetOps.SharedKernel.Domain;

namespace FleetOps.Stock.Domain;

public static class StockErrors
{
    public static readonly Error LokasyonKoduBos =
        new("Stock.LokasyonKoduBos", "Lokasyon kodu boş olamaz.");

    public static readonly Error MalzemeKoduBos =
        new("Stock.MalzemeKoduBos", "Malzeme kodu boş olamaz.");

    public static readonly Error MiktarPozitifOlmali =
        new("Stock.MiktarPozitifOlmali", "Miktar sıfırdan büyük olmalıdır.");

    public static readonly Error AyniLokasyon =
        new("Stock.AyniLokasyon", "Kaynak ve hedef lokasyon aynı olamaz.");
}
