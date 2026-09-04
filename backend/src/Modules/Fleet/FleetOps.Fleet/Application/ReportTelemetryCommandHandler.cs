using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Application;

internal sealed class ReportTelemetryCommandHandler(
    FleetDbContext db,
    IFleetNotifier notifier)
    : ICommandHandler<ReportTelemetryCommand>
{
    public async Task<Result> HandleAsync(
        ReportTelemetryCommand command,
        CancellationToken cancellationToken)
    {
        var agv = await db.Agvs
            .FirstOrDefaultAsync(a => a.Id == command.AgvId, cancellationToken);

        if (agv is null)
        {
            return Result.Failure(FleetErrors.Bulunamadi);
        }

        var sonuc = agv.TelemetriBildir(command.BatteryLevel, command.LocationId, DateTime.UtcNow);
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
            // Telemetri tekrar eden bir sinyal: kaybedilen ornegin yerini bir
            // sonraki aliyor. Yeniden denemek, eskimis olcumu tazenin uzerine
            // yazma riski tasir.
            return Result.Failure(FleetErrors.EszamanliDegisiklik);
        }

        await notifier.AgvDegistiAsync(AgvSummary.Olustur(agv), cancellationToken);
        return Result.Success();
    }
}
