namespace FleetOps.SharedKernel.IntegrationEvents;

// Tasks -> Fleet. Bir gorev bir AGV'ye atandi.
public sealed record TaskAssignedIntegrationEvent(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid TaskId,
    Guid AgvId) : IntegrationEvent(Id, OccurredAtUtc);
