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

public sealed record ListStockMovementsQuery(PageRequest Sayfa)
    : IQuery<PagedResult<StockMovementSummary>>;

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
    : IQueryHandler<ListStockMovementsQuery, PagedResult<StockMovementSummary>>
{
    public async Task<Result<PagedResult<StockMovementSummary>>> HandleAsync(
        ListStockMovementsQuery query,
        CancellationToken cancellationToken)
    {
        var toplam = await db.StockMovements.CountAsync(cancellationToken);

        var kayitlar = await db.StockMovements
            .AsNoTracking()
            .OrderByDescending(m => m.MovedAtUtc)
            // Ayni transaction'da olusan hareketlerin MovedAtUtc'si esit
            // olabilir. Esitlik bozucu olmadan OFFSET/LIMIT kayar; olculdu
            // (2026-09-04): 200k satirda 40 kayittan 39'u kapsandi.
            .ThenBy(m => m.Id)
            .Select(m => new StockMovementSummary(
                m.Id,
                m.MaterialCode,
                m.Quantity,
                db.Locations.Where(l => l.Id == m.FromLocationId).Select(l => l.Code).First(),
                db.Locations.Where(l => l.Id == m.ToLocationId).Select(l => l.Code).First(),
                m.SourceTaskId,
                m.MovedAtUtc))
            .Skip(query.Sayfa.Atlanacak)
            .Take(query.Sayfa.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<StockMovementSummary>(
            kayitlar, query.Sayfa.Page, query.Sayfa.PageSize, toplam));
    }
}
