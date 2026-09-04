using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FleetOps.Tasks.Application;

internal sealed class AcquireLockCommandHandler(
    TasksDbContext db,
    IOptions<ResourceLockOptions> options) : ICommandHandler<AcquireLockCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        AcquireLockCommand command,
        CancellationToken cancellationToken)
    {
        var kaynakVar = await db.Resources
            .AnyAsync(r => r.Id == command.ResourceId, cancellationToken);

        if (!kaynakVar)
        {
            return Result.Failure<Guid>(ResourceErrors.Bulunamadi);
        }

        var sonuc = ResourceLock.Acquire(
            Guid.NewGuid(),
            command.ResourceId,
            command.AgvId,
            DateTime.UtcNow,
            options.Value.Duration);

        if (sonuc.IsFailure)
        {
            return Result.Failure<Guid>(sonuc.Error);
        }

        db.ResourceLocks.Add(sonuc.Value);

        // Once "aktif kilit var mi" diye sorup sonra yazmak yaris acar: iki
        // sorgunun arasina sigan ucuncu istek ikisini de gecirirdi.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
                  { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Result.Failure<Guid>(ResourceErrors.KaynakMesgul);
        }

        return Result.Success(sonuc.Value.Id);
    }
}
