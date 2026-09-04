using FleetOps.Fleet.Application;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
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
        // Zaten mesgulse degisiklik yok: ayni olay tekrar teslim edilmis olabilir.
        var bulundu = await AgvGuncelleyici.GuncelleAsync(
            db, notifier, olay.AgvId, agv => agv.Mesgullestir().IsSuccess, cancellationToken);

        if (!bulundu)
        {
            logger.LogWarning("Atama olayindaki AGV bulunamadi: {AgvId}", olay.AgvId);
        }
    }
}
