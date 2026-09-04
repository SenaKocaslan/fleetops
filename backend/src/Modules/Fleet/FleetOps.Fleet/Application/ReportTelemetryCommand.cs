using FleetOps.SharedKernel;

namespace FleetOps.Fleet.Application;

public sealed record ReportTelemetryCommand(
    Guid AgvId,
    int BatteryLevel,
    Guid? LocationId) : ICommand;
