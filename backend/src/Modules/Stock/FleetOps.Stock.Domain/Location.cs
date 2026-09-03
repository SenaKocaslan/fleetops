using FleetOps.SharedKernel.Domain;

namespace FleetOps.Stock.Domain;

// Depodaki fiziksel bir konum: raf, kabul alani, sevkiyat alani.
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
