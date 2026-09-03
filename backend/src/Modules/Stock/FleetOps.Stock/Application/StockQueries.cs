using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Stock.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Stock.Application;

public sealed record LocationSummary(Guid Id, string Code, string Zone);

public sealed record StockMovementSummary(
    Guid Id,
    string MaterialCode,
    int Quantity,
    string FromLocationCode,
    string ToLocationCode,
    Guid SourceTaskId,
    DateTime MovedAtUtc);

public sealed record ListLocationsQuery : IQuery<IReadOnlyList<LocationSummary>>;

public sealed record ListStockMovementsQuery : IQuery<IReadOnlyList<StockMovementSummary>>;

internal sealed class ListLocationsQueryHandler(StockDbContext db)
    : IQueryHandler<ListLocationsQuery, IReadOnlyList<LocationSummary>>
{
    public async Task<Result<IReadOnlyList<LocationSummary>>> HandleAsync(
        ListLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var kayitlar = await db.Locations
            .AsNoTracking()
            .OrderBy(l => l.Code)
            .Select(l => new LocationSummary(l.Id, l.Code, l.Zone))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LocationSummary>>(kayitlar);
    }
}

internal sealed class ListStockMovementsQueryHandler(StockDbContext db)
    : IQueryHandler<ListStockMovementsQuery, IReadOnlyList<StockMovementSummary>>
{
    public async Task<Result<IReadOnlyList<StockMovementSummary>>> HandleAsync(
        ListStockMovementsQuery query,
        CancellationToken cancellationToken)
    {
        // Lokasyon kodlari ayni modulde oldugu icin dogrudan birlestiriliyor.
        var kayitlar = await db.StockMovements
            .AsNoTracking()
            .OrderByDescending(m => m.MovedAtUtc)
            .Select(m => new StockMovementSummary(
                m.Id,
                m.MaterialCode,
                m.Quantity,
                db.Locations.Where(l => l.Id == m.FromLocationId).Select(l => l.Code).First(),
                db.Locations.Where(l => l.Id == m.ToLocationId).Select(l => l.Code).First(),
                m.SourceTaskId,
                m.MovedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<StockMovementSummary>>(kayitlar);
    }
}
