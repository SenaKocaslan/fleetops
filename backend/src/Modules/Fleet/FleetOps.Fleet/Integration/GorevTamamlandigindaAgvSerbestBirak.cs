using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;

namespace FleetOps.Fleet.Integration;

internal sealed class GorevTamamlandigindaAgvSerbestBirak(
    FleetDbContext db,
    IFleetNotifier notifier)
    : IntegrationEventHandler<TaskCompletedIntegrationEvent>
{
    protected override Task HandleAsync(
        TaskCompletedIntegrationEvent olay,
        CancellationToken cancellationToken) =>
        AgvGuncelleyici.GuncelleAsync(db, notifier, olay.AgvId, SerbestBirak, cancellationToken);

    // Mesgul degilse yazacak bir sey yok; gereksiz UPDATE telemetriyle
    // catisma ihtimalini artirirdi.
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
