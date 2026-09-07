using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Application;

internal sealed class ReleaseLockCommandHandler(TasksDbContext db)
    : ICommandHandler<ReleaseLockCommand>
{
    public async Task<Result> HandleAsync(
        ReleaseLockCommand command,
        CancellationToken cancellationToken)
    {
        var kilit = await db.ResourceLocks.FirstOrDefaultAsync(
            l => l.ResourceId == command.ResourceId && l.ReleasedAtUtc == null,
            cancellationToken);

        if (kilit is null)
        {
            return Result.Failure(ResourceErrors.KilitBulunamadi);
        }

        var sonuc = kilit.Release(command.AgvId, DateTime.UtcNow);
        if (sonuc.IsFailure)
        {
            return sonuc;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
