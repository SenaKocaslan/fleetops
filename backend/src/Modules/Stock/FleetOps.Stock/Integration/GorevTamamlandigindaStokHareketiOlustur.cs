using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
using FleetOps.Stock.Domain;
using FleetOps.Stock.Infrastructure;
using FleetOps.Stock.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetOps.Stock.Integration;

internal sealed class GorevTamamlandigindaStokHareketiOlustur(
    StockDbContext db,
    ILogger<GorevTamamlandigindaStokHareketiOlustur> logger)
    : IntegrationEventHandler<TaskCompletedIntegrationEvent>
{
    protected override async Task HandleAsync(
        TaskCompletedIntegrationEvent olay,
        CancellationToken cancellationToken)
    {
        // Bu kontrol optimizasyon, kuralin kendisi degil: asil bekci
        // processed_integration_event'in birincil anahtari.
        var islenmis = await db.ProcessedEvents
            .AnyAsync(e => e.Id == olay.Id, cancellationToken);

        if (islenmis)
        {
            return;
        }

        var hareket = StockMovement.Create(
            Guid.NewGuid(),
            olay.MaterialCode,
            olay.Quantity,
            olay.FromLocationId,
            olay.ToLocationId,
            olay.TaskId,
            olay.OccurredAtUtc);

        if (hareket.IsFailure)
        {
            logger.LogError(
                "Stok hareketi olusturulamadi ({Kod}): {Mesaj}",
                hareket.Error.Code, hareket.Error.Message);
        }
        else
        {
            db.StockMovements.Add(hareket.Value);
        }

        db.ProcessedEvents.Add(new ProcessedIntegrationEvent(olay.Id, DateTime.UtcNow));

        // Hareket ile "islendi" isareti ayni transaction'da yazilmali.
        await db.SaveChangesAsync(cancellationToken);
    }
}
