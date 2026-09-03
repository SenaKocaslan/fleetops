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
        // Yazma yolu: burada projeksiyon degil aggregate yuklenir, cunku
        // gecis kurali aggregate'in icinde. Assignments da yukleniyor;
        // aksi halde AktifAtama her zaman null gorunurdu.
        var gorev = await db.TransportTasks
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
            // UPDATE ... WHERE id = @id AND xmin = @okunan_xmin sifir satir
            // etkiledi: goreve biz okuduktan sonra baskasi yazdi. Beklenen
            // bir durum, bu yuzden exception disari sizmiyor.
            return Result.Failure(TaskErrors.EszamanliDegisiklik);
        }

        return Result.Success();
    }
}
