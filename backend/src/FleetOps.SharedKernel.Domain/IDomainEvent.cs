namespace FleetOps.SharedKernel.Domain;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
