namespace FleetOps.Fleet.Application;

// Atama ekraninin ihtiyaci kadar alan. Aggregate donulmez.
public sealed record AgvSummary(
    Guid Id,
    string Code,
    string Status,
    int BatteryLevel,
    bool GorevAlabilir);
