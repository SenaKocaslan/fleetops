using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;

namespace FleetOps.Tasks.Application;

internal sealed class CreateTaskCommandHandler(TasksDbContext db)
    : ICommandHandler<CreateTaskCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken)
    {
        // Dogrulama aggregate'in icinde; handler kural tekrar etmez.
        var sonuc = TransportTask.Create(
            Guid.NewGuid(),
            command.FromLocationId,
            command.ToLocationId,
            command.MaterialCode,
            command.Quantity,
            command.Priority,
            DateTime.UtcNow);

        if (sonuc.IsFailure)
        {
            return Result.Failure<Guid>(sonuc.Error);
        }

        db.TransportTasks.Add(sonuc.Value);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(sonuc.Value.Id);
    }
}
