namespace FleetOps.Stock.Infrastructure;

public sealed class ProcessedIntegrationEvent
{
    public ProcessedIntegrationEvent(Guid id, DateTime processedAtUtc)
    {
        Id = id;
        ProcessedAtUtc = processedAtUtc;
    }

    private ProcessedIntegrationEvent()
    {
    }

    public Guid Id { get; private set; }

    public DateTime ProcessedAtUtc { get; private set; }
}
