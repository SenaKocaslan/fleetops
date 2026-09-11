using FleetOps.SharedKernel.Domain;

namespace FleetOps.Stock.Domain;

public sealed class Location : AggregateRoot
{
    private Location(Guid id, string code, string zone) : base(id)
    {
        Code = code;
        Zone = zone;
    }

    private Location()
    {
        Code = string.Empty;
        Zone = string.Empty;
    }

    public string Code { get; private set; }

    public string Zone { get; private set; }

    public static Result<Location> Create(Guid id, string code, string zone)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Location>(StockErrors.LokasyonKoduBos);
        }

        return Result.Success(new Location(id, code.Trim(), zone.Trim()));
    }
}

// Sistem depo ICI tasimayi izliyor. Kabul alanina tedarikciden gelen ve
// sevkiyat alanindan musteriye giden malzeme sisteme hic girmiyor; bu iki
// bolge bir SINIR. Oradaki bakiye hareketlerden hesaplanamaz: olculdu
// (2026-09-11), KABUL-01 -182 cikiyordu -- oradan hep cikis var, giris hic
// yok. Bakiye yalnizca Depo bolgesinde anlamli.
public static class Bolgeler
{
    public const string Kabul = "Kabul";
    public const string Depo = "Depo";
    public const string Sevkiyat = "Sevkiyat";
}
