namespace FleetOps.Fleet.Application;

// Fleet modulunun tasima teknolojisinden (SignalR) bagimsiz kalmasi icin.
// Handler'lar bunu cagirir; SignalR bilgisi tek bir implementasyonda toplanir.
public interface IFleetNotifier
{
    Task AgvDegistiAsync(AgvSummary agv, CancellationToken cancellationToken);
}
