namespace FleetOps.Tasks.Application;

public sealed record ResourceSummary(
    Guid Id,
    string Code,
    string Kind,
    Guid? LockedByAgvId,
    DateTime? LockExpiresAtUtc);
