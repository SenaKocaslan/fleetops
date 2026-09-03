namespace FleetOps.Tasks.Application;

// Kaynak listesi ekraninin ihtiyaci kadar alan.
public sealed record ResourceSummary(
    Guid Id,
    string Code,
    string Kind,
    Guid? LockedByAgvId,
    DateTime? LockExpiresAtUtc);
