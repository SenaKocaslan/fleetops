using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Tasks.Application;

internal abstract class TaskGecisHandler(TasksDbContext db)
{
    protected async Task<Result> GecisUygulaAsync(
        Guid taskId,
        Func<TransportTask, Result> gecis,
        CancellationToken cancellationToken)
    {
        var gorev = await db.TransportTasks
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (gorev is null)
        {
            return Result.Failure(TaskErrors.Bulunamadi);
        }

        var sonuc = gecis(gorev);
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

internal sealed class StartTaskCommandHandler(TasksDbContext db)
    : TaskGecisHandler(db), ICommandHandler<StartTaskCommand>
{
    public Task<Result> HandleAsync(StartTaskCommand command, CancellationToken cancellationToken) =>
        GecisUygulaAsync(command.TaskId, gorev => gorev.Start(), cancellationToken);
}

internal sealed class CompleteTaskCommandHandler(TasksDbContext db)
    : TaskGecisHandler(db), ICommandHandler<CompleteTaskCommand>
{
    public Task<Result> HandleAsync(CompleteTaskCommand command, CancellationToken cancellationToken) =>
        GecisUygulaAsync(command.TaskId, gorev => gorev.Complete(DateTime.UtcNow), cancellationToken);
}

internal sealed class ReleaseTaskCommandHandler(TasksDbContext db)
    : TaskGecisHandler(db), ICommandHandler<ReleaseTaskCommand>
{
    public Task<Result> HandleAsync(ReleaseTaskCommand command, CancellationToken cancellationToken) =>
        GecisUygulaAsync(command.TaskId, gorev => gorev.Release(DateTime.UtcNow), cancellationToken);
}

internal sealed class FailTaskCommandHandler(TasksDbContext db)
    : TaskGecisHandler(db), ICommandHandler<FailTaskCommand>
{
    public Task<Result> HandleAsync(FailTaskCommand command, CancellationToken cancellationToken) =>
        GecisUygulaAsync(command.TaskId, gorev => gorev.Fail(DateTime.UtcNow), cancellationToken);
}

internal sealed class CancelTaskCommandHandler(TasksDbContext db)
    : TaskGecisHandler(db), ICommandHandler<CancelTaskCommand>
{
    public Task<Result> HandleAsync(CancelTaskCommand command, CancellationToken cancellationToken) =>
        GecisUygulaAsync(command.TaskId, gorev => gorev.Cancel(), cancellationToken);
}
