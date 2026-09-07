using System.Text.Json;
using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Infrastructure;

public sealed class OutboxMessage
{
    private OutboxMessage(Guid id, string type, string payload, DateTime occurredAtUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    private OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Olustur(IntegrationEvent olay) =>
        new(olay.Id,
            IntegrationEventTypeRegistry.Ad(olay),
            JsonSerializer.Serialize(olay, olay.GetType()),
            olay.OccurredAtUtc);

    public void Islendi(DateTime nowUtc)
    {
        ProcessedAtUtc = nowUtc;
        Error = null;
    }

    public void Basarisiz(string hata) => Error = hata;
}
