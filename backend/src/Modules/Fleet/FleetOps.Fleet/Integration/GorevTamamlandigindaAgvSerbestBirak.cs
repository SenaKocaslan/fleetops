using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Integration;

// Gorev tamamlaninca AGV yeniden musait olur.
internal sealed class GorevTamamlandigindaAgvSerbestBirak(FleetDbContext db)
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

        // SerbestBirak yalnizca mesgulse etki eder; ikinci teslim
        // hicbir sey degistirmez. Ayrica idempotentlik tablosu gerekmiyor:
        // "mesgul degil" durumuna gecmek tekrarlanabilir bir islem.
        agv.SerbestBirak();
        await db.SaveChangesAsync(cancellationToken);
    }
}
