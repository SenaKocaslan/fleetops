using FleetOps.Fleet.Domain;

namespace FleetOps.Fleet.Application;

public sealed record AgvSummary(
    Guid Id,
    string Code,
    string Status,
    int BatteryLevel,
    bool GorevAlabilir,
    Guid? CurrentLocationId,
    DateTime? LastSeenAtUtc)
{
    public static AgvSummary Olustur(Agv agv) => new(
        agv.Id,
        agv.Code,
        agv.Status.ToString(),
        agv.BatteryLevel,
        agv.GorevAlabilir(),
        agv.CurrentLocationId,
        agv.LastSeenAtUtc);
}
