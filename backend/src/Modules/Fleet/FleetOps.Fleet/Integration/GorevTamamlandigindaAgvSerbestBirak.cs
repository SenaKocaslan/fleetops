using FleetOps.Fleet.Application;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Integration;

internal sealed class GorevTamamlandigindaAgvSerbestBirak(
    FleetDbContext db,
    IFleetNotifier notifier)
    : IntegrationEventHandler<TaskCompletedIntegrationEvent>
{
    protected override async Task HandleAsync(
        TaskCompletedIntegrationEvent olay,
        CancellationToken cancellationToken)
    {
        var agv = await db.Agvs.FirstOrDefaultAsync(a => a.Id == olay.AgvId, cancellationToken);

        if (agv is null)
        {
            return;
        }

        agv.SerbestBirak();
        await db.SaveChangesAsync(cancellationToken);

        await notifier.AgvDegistiAsync(AgvSummary.Olustur(agv), cancellationToken);
    }
}
