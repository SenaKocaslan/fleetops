using FleetOps.Fleet.Application;
using Microsoft.AspNetCore.SignalR;

namespace FleetOps.Fleet.Infrastructure;

internal sealed class SignalRFleetNotifier(IHubContext<FleetHub> hub) : IFleetNotifier
{
    public Task AgvDegistiAsync(AgvSummary agv, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(FleetHub.AgvDegisti, agv, cancellationToken);
}
