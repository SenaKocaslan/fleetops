namespace FleetOps.Tasks.Application;

public sealed record TaskSummary(
    Guid Id,
    string Status,
    string MaterialCode,
    int Quantity,
    int Priority,
    DateTime CreatedAtUtc,
    Guid? AssignedAgvId);
