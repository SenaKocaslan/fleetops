using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        // Nazik kontrol: bilinen bir cakismada kullaniciya anlamli hata donmek
        // icin. Kuralin asil bekcisi task_assignment uzerindeki kismi tekil
        // indeks; bu sorgu ile INSERT arasinda baska bir istek araya girebilir.
        var agvMesgul = await db.TransportTasks
            .AsNoTracking()
            .SelectMany(t => t.Assignments)
            .AnyAsync(a => a.AgvId == command.AgvId && a.CompletedAtUtc == null, cancellationToken);

        if (agvMesgul)
        {
            return Result.Failure(TaskErrors.AgvMesgul);
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
        catch (DbUpdateException ex) when (TekilIhlali(ex))
        {
            // Yukaridaki kontrolu gecen ikinci istek buraya duser: iki istek
            // ayni AGV'yi ayni anda aldi, indeks ikincisini reddetti.
            return Result.Failure(TaskErrors.AgvMesgul);
        }

        return Result.Success();
    }

    // Npgsql tekil kisit ihlalini 23505 ile bildiriyor. Mesaj metnine bakmak
    // yerine kodu kontrol ediyoruz; metin dil ve surume gore degisir.
    private static bool TekilIhlali(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
