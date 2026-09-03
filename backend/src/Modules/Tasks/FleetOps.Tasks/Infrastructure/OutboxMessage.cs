using System.Text.Json;
using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Infrastructure;

// Gonderilmeyi bekleyen integration event. Durum degisikligiyle AYNI
// transaction'da yazilir: ya ikisi birden olur ya hicbiri. "Kayit gitti
// ama olay gitmedi" durumu bu yuzden imkansiz.
// Domain projesinde degil, cunku bir is kavrami degil - teslimat mekanizmasi.
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

    // Integration event'in kendi kimligi. Tuketici tekrari bununla anlar.
    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    // Basarisiz denemenin sebebi; satir islenmemis olarak kalir.
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

    // Islenmis olarak isaretlenmez: bir sonraki turda tekrar denenir.
    public void Basarisiz(string hata) => Error = hata;
}
