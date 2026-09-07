namespace FleetOps.SharedKernel.IntegrationEvents;

public sealed record TaskAssignedIntegrationEvent(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid TaskId,
    Guid AgvId) : IntegrationEvent(Id, OccurredAtUtc);
