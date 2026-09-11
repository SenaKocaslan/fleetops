using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.SharedKernel.IntegrationEvents;
using FleetOps.Tasks.Domain;

namespace FleetOps.Tasks.Infrastructure;

internal static class IntegrationEventFactory
{
    public static IntegrationEvent? Olustur(IDomainEvent domainEvent) => domainEvent switch
    {
        TaskAssignedDomainEvent e => new TaskAssignedIntegrationEvent(
            Guid.NewGuid(), e.OccurredAtUtc, e.TaskId, e.AgvId),

        TaskCompletedDomainEvent e => new TaskCompletedIntegrationEvent(
            Guid.NewGuid(),
            e.OccurredAtUtc,
            e.TaskId,
            e.AgvId,
            e.MaterialCode,
            e.Quantity,
            e.FromLocationId,
            e.ToLocationId),

        TaskAssignmentEndedDomainEvent e => new TaskAssignmentEndedIntegrationEvent(
            Guid.NewGuid(), e.OccurredAtUtc, e.TaskId, e.AgvId, e.Sebep),

        _ => null,
    };
}
