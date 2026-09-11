using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;

namespace FleetOps.Fleet.Integration;

// Gorev havuza donduruldu ya da basarisiz oldu: arac artik o gorevde degil.
// Tamamlanma olayindaki tuketiciyle ayni is, farkli olay; ikisi de olay
// kimligiyle bir kez isleniyor.
internal sealed class GorevAtamasiBittigindeAgvSerbestBirak(
    FleetDbContext db,
    IFleetNotifier notifier)
    : IntegrationEventHandler<TaskAssignmentEndedIntegrationEvent>
{
    protected override Task HandleAsync(
        TaskAssignmentEndedIntegrationEvent olay,
        CancellationToken cancellationToken) =>
        AgvGuncelleyici.GuncelleAsync(
            db, notifier, olay.AgvId, SerbestBirak, cancellationToken, olay.Id);

    private static bool SerbestBirak(Agv agv)
    {
        if (agv.Status != AgvStatus.Busy)
        {
            return false;
        }

        agv.SerbestBirak();
        return true;
    }
}
