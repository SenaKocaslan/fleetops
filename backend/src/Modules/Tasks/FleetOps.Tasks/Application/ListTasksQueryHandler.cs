using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Application;

internal sealed class ListTasksQueryHandler(TasksDbContext db)
    : IQueryHandler<ListTasksQuery, PagedResult<TaskSummary>>
{
    public async Task<Result<PagedResult<TaskSummary>>> HandleAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken)
    {
        var sorgu = db.TransportTasks.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.MaterialCode))
        {
            var ara = query.MaterialCode.Trim();
            sorgu = sorgu.Where(t => EF.Functions.ILike(t.MaterialCode, $"%{ara}%"));
        }

        // Toplam sayi filtreden SONRA hesaplanmali; aksi halde sayfa sayisi
        // gosterilenden fazla cikar.
        var toplam = await sorgu.CountAsync(cancellationToken);

        var kayitlar = await sorgu
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAtUtc)
            // Id sadece esitlik bozucu ve sart. Olculdu (2026-09-04): 200k
            // satir + paralel planda, bozucusuz sorguda 40 satirin 39'u
            // kapsandi -- bir kayit iki sayfada birden, bir kayit hic yok.
            // Kucuk veride tetiklenmiyor, bu yuzden testler yakalamiyor.
            .ThenBy(t => t.Id)
            .Select(t => new TaskSummary(
                t.Id,
                t.Status.ToString(),
                t.MaterialCode,
                t.Quantity,
                t.Priority,
                t.CreatedAtUtc,
                t.Assignments
                    .Where(a => a.CompletedAtUtc == null)
                    .Select(a => (Guid?)a.AgvId)
                    .FirstOrDefault()))
            .Skip(query.Sayfa.Atlanacak)
            .Take(query.Sayfa.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<TaskSummary>(
            kayitlar, query.Sayfa.Page, query.Sayfa.PageSize, toplam));
    }
}
