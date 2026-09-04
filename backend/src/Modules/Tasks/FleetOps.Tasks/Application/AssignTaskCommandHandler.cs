using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Application;

internal sealed class AssignTaskCommandHandler(TasksDbContext db)
    : ICommandHandler<AssignTaskCommand>
{
    public async Task<Result> HandleAsync(
        AssignTaskCommand command,
        CancellationToken cancellationToken)
    {
        var gorev = await db.TransportTasks
            // Include olmadan AktifAtama her zaman null gorunur.
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (gorev is null)
        {
            return Result.Failure(TaskErrors.Bulunamadi);
        }

        var sonuc = gorev.Assign(command.AgvId, DateTime.UtcNow);
        if (sonuc.IsFailure)
        {
            return sonuc;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(TaskErrors.EszamanliDegisiklik);
        }

        return Result.Success();
    }
}
