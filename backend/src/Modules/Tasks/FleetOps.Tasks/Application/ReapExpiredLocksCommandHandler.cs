using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Application;

internal sealed class ReapExpiredLocksCommandHandler(TasksDbContext db)
    : ICommandHandler<ReapExpiredLocksCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        ReapExpiredLocksCommand command,
        CancellationToken cancellationToken)
    {
        var simdi = DateTime.UtcNow;

        var kilitler = await db.ResourceLocks
            .Where(l => l.ReleasedAtUtc == null && l.ExpiresAtUtc <= simdi)
            .ToListAsync(cancellationToken);

        var birakilan = 0;
        foreach (var kilit in kilitler)
        {
            // Karari yine aggregate veriyor; sorgu ile aggregate'in fikri
            // ayrisirsa aggregate kazanir.
            if (kilit.ZamanAsimiylaBirak(simdi).IsSuccess)
            {
                birakilan++;
            }
        }

        if (birakilan > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(birakilan);
    }
}
