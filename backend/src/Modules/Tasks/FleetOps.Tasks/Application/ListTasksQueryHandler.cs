using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Application;

internal sealed class ListTasksQueryHandler(TasksDbContext db)
    : IQueryHandler<ListTasksQuery, IReadOnlyList<TaskSummary>>
{
    public async Task<Result<IReadOnlyList<TaskSummary>>> HandleAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken)
    {
        // AsNoTracking: okuma yolunda change tracker'a ihtiyac yok.
        var kayitlar = await db.TransportTasks
            .AsNoTracking()
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAtUtc)
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
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TaskSummary>>(kayitlar);
    }
}
