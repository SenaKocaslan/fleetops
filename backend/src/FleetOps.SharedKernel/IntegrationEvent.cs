namespace FleetOps.SharedKernel;

// Moduller arasi iletisimin tasiyicisi. Id, teslimin tekrarlandigini
// anlayabilmek icin: ayni olay iki kez gelirse Id ayni olur.
public abstract record IntegrationEvent(Guid Id, DateTime OccurredAtUtc);

// Tek bir olay turunu isleyen tuketicinin sozlesmesi. Dagitici olayin
// somut turunu derleme zamaninda bilmedigi icin once bu arayuzu gorur.
public interface IIntegrationEventHandler
{
    Type EventType { get; }

    Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

// Tuketiciler bunu miras alir; tur donusumu tek yerde kalir ve dagitim
// aninda reflection kullanilmaz.
public abstract class IntegrationEventHandler<TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    public Type EventType => typeof(TEvent);

    public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)integrationEvent, cancellationToken);

    protected abstract Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
