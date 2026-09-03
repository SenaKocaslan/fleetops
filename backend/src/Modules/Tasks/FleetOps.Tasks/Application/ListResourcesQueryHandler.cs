using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Application;

internal sealed class ListResourcesQueryHandler(TasksDbContext db)
    : IQueryHandler<ListResourcesQuery, IReadOnlyList<ResourceSummary>>
{
    public async Task<Result<IReadOnlyList<ResourceSummary>>> HandleAsync(
        ListResourcesQuery query,
        CancellationToken cancellationToken)
    {
        // Aktif kilit sol birlestirme ile geliyor; kismi tekil indeks
        // sayesinde kaynak basina en fazla bir tane olabilecegini biliyoruz.
        var kayitlar = await db.Resources
            .AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new ResourceSummary(
                r.Id,
                r.Code,
                r.Kind.ToString(),
                db.ResourceLocks
                    .Where(l => l.ResourceId == r.Id && l.ReleasedAtUtc == null)
                    .Select(l => (Guid?)l.AgvId)
                    .FirstOrDefault(),
                db.ResourceLocks
                    .Where(l => l.ResourceId == r.Id && l.ReleasedAtUtc == null)
                    .Select(l => (DateTime?)l.ExpiresAtUtc)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ResourceSummary>>(kayitlar);
    }
}
