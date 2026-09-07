namespace FleetOps.SharedKernel;

public abstract record IntegrationEvent(Guid Id, DateTime OccurredAtUtc);

public interface IIntegrationEventHandler
{
    Type EventType { get; }

    Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

public abstract class IntegrationEventHandler<TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    public Type EventType => typeof(TEvent);

    public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)integrationEvent, cancellationToken);

    protected abstract Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
