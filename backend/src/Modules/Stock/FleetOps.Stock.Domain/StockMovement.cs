using FleetOps.SharedKernel.Domain;

namespace FleetOps.Stock.Domain;

// Gerceklesmis bir malzeme hareketi. Gecmis kaydidir: olusturulduktan
// sonra degismez, bu yuzden durum degistiren metodu yok.
public sealed class StockMovement : AggregateRoot
{
    private StockMovement(
        Guid id,
        string materialCode,
        int quantity,
        Guid fromLocationId,
        Guid toLocationId,
        Guid sourceTaskId,
        DateTime movedAtUtc) : base(id)
    {
        MaterialCode = materialCode;
        Quantity = quantity;
        FromLocationId = fromLocationId;
        ToLocationId = toLocationId;
        SourceTaskId = sourceTaskId;
        MovedAtUtc = movedAtUtc;
    }

    private StockMovement()
    {
        MaterialCode = string.Empty;
    }

    public string MaterialCode { get; private set; }

    public int Quantity { get; private set; }

    public Guid FromLocationId { get; private set; }

    public Guid ToLocationId { get; private set; }

    // Tasks modulundeki gorevin kimligi. Foreign key DEGIL.
    public Guid SourceTaskId { get; private set; }

    public DateTime MovedAtUtc { get; private set; }

    public static Result<StockMovement> Create(
        Guid id,
        string materialCode,
        int quantity,
        Guid fromLocationId,
        Guid toLocationId,
        Guid sourceTaskId,
        DateTime movedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(materialCode))
        {
            return Result.Failure<StockMovement>(StockErrors.MalzemeKoduBos);
        }

        if (quantity <= 0)
        {
            return Result.Failure<StockMovement>(StockErrors.MiktarPozitifOlmali);
        }

        if (fromLocationId == toLocationId)
        {
            return Result.Failure<StockMovement>(StockErrors.AyniLokasyon);
        }

        return Result.Success(new StockMovement(
            id, materialCode.Trim(), quantity, fromLocationId, toLocationId, sourceTaskId, movedAtUtc));
    }
}
