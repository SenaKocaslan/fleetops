using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Application;

internal sealed class ListAgvsQueryHandler(FleetDbContext db)
    : IQueryHandler<ListAgvsQuery, IReadOnlyList<AgvSummary>>
{
    public async Task<Result<IReadOnlyList<AgvSummary>>> HandleAsync(
        ListAgvsQuery query,
        CancellationToken cancellationToken)
    {
        var agvler = await db.Agvs
            .AsNoTracking()
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);

        var sonuc = agvler.Select(AgvSummary.Olustur).ToList();

        return Result.Success<IReadOnlyList<AgvSummary>>(sonuc);
    }
}
