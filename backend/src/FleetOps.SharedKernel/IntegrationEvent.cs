namespace FleetOps.SharedKernel;

public abstract record IntegrationEvent(Guid Id, DateTime OccurredAtUtc);
