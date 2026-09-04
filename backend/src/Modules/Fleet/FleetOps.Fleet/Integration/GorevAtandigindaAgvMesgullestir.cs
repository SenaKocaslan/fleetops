using FleetOps.Fleet.Application;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetOps.Fleet.Integration;

internal sealed class GorevAtandigindaAgvMesgullestir(
    FleetDbContext db,
    IFleetNotifier notifier,
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
            logger.LogWarning("Atama olayindaki AGV bulunamadi: {AgvId}", olay.AgvId);
            return;
        }

        // Zaten mesgulse hata degil: ayni olay tekrar teslim edilmis olabilir.
        if (agv.Mesgullestir().IsFailure)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);

        await notifier.AgvDegistiAsync(AgvSummary.Olustur(agv), cancellationToken);
    }
}
