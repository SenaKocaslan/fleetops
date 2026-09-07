namespace FleetOps.SharedKernel.IntegrationEvents;

public sealed record TaskCompletedIntegrationEvent(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid TaskId,
    Guid AgvId,
    string MaterialCode,
    int Quantity,
    Guid FromLocationId,
    Guid ToLocationId) : IntegrationEvent(Id, OccurredAtUtc);
