using FleetOps.SharedKernel;
using FleetOps.SharedKernel.Domain;
using FleetOps.SharedKernel.IntegrationEvents;
using FleetOps.Tasks.Domain;

namespace FleetOps.Tasks.Infrastructure;

// Ic domain event -> disa acik integration event cevrimi TEK yerde.
// Karsiligi olmayan domain event'ler null doner ve disari cikmaz;
// her domain event'in dis dunyayi ilgilendirmesi gerekmiyor.
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

        _ => null,
    };
}
