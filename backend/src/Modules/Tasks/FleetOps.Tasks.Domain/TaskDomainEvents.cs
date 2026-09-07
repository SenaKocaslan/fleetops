using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public sealed record TaskAssignedDomainEvent(
    Guid TaskId,
    Guid AgvId,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record TaskCompletedDomainEvent(
    Guid TaskId,
    Guid AgvId,
    string MaterialCode,
    int Quantity,
    Guid FromLocationId,
    Guid ToLocationId,
    DateTime OccurredAtUtc) : IDomainEvent;
