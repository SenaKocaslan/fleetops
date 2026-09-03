using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetOps.Fleet.Integration;

// Tasks modulu bir gorevi atadiginda AGV mesgule alinir.
// Bu kontrol Gun 4'te bilerek yapilmamisti: Tasks modulu Fleet'i goremez.
// Iletisimin dogru yeri burasi - Fleet olayi dinliyor, Tasks Fleet'i
// cagirmiyor.
internal sealed class GorevAtandigindaAgvMesgullestir(
    FleetDbContext db,
    ILogger<GorevAtandigindaAgvMesgullestir> logger)
    : IntegrationEventHandler<TaskAssignedIntegrationEvent>
{
    protected override async Task HandleAsync(
        TaskAssignedIntegrationEvent olay,
        CancellationToken cancellationToken)
    {
        var agv = await db.Agvs.FirstOrDefaultAsync(a => a.Id == olay.AgvId, cancellationToken);

        if (agv is null)
        {
            // Olay atilmaz: olmayan bir AGV icin tekrar denemek bir sey
            // degistirmez, yalnizca kuyrugu tikar.
            logger.LogWarning("Atama olayindaki AGV bulunamadi: {AgvId}", olay.AgvId);
            return;
        }

        // IDEMPOTENT: teslimat en az bir kez oldugu icin ayni olay tekrar
        // gelebilir. AGV zaten mesgulse aggregate reddediyor ve bu bir hata
        // degil - ikinci teslim hicbir sey degistirmemis oluyor.
        if (agv.Mesgullestir().IsFailure)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
