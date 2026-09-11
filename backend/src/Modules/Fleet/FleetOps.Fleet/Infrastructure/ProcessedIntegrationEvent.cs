namespace FleetOps.Fleet.Infrastructure;

// Stock modulundekiyle ayni kalip; moduller birbirinin tipini goremedigi
// ve her modul kendi kaliciligina sahip oldugu icin ayri bir sinif.
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
