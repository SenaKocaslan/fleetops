using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
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
        var reddedildi = false;

        // Zaten mesgulse degisiklik yok: ayni olay tekrar teslim edilmis olabilir.
        var bulundu = await AgvGuncelleyici.GuncelleAsync(db, notifier, olay.AgvId, agv =>
        {
            if (agv.Status == AgvStatus.Busy)
            {
                return false;
            }

            var sonuc = agv.Mesgullestir();
            reddedildi = sonuc.IsFailure;

            return sonuc.IsSuccess;
        }, cancellationToken);

        if (!bulundu)
        {
            logger.LogWarning("Atama olayindaki AGV bulunamadi: {AgvId}", olay.AgvId);
            return;
        }

        if (reddedildi)
        {
            // Atama ile bu olayin teslimi arasinda arac sarja girmis ya da
            // servis disi kalmis olabilir. Gorev artik kimsenin beklemedigi
            // bir yerde asili: Tasks tarafindaki "BaslamayanGorev" alarmi
            // bunu gorunur kiliyor. Burada sessiz kalmak en kotusuydu.
            logger.LogWarning(
                "AGV atamayi alamadi, gorev askida kalabilir: {AgvId} / gorev {GorevId}",
                olay.AgvId, olay.TaskId);
        }
    }
}
