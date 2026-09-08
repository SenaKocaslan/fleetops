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

    public int AttemptCount { get; private set; }

    // Dolu ise mesaj kuyruktan cikarilmistir: dagitici onu bir daha almaz.
    // Satir silinmiyor, cunku hatanin ne oldugu ve kac kez denendigi
    // sorusunun cevabi yalnizca burada duruyor.
    public DateTime? DeadLetteredAtUtc { get; private set; }

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

    // Islenmis isaretlenmez; mesaj bir sonraki turda tekrar denenir. Ama
    // sonsuza kadar degil: her turda bastan patlayan bir mesaj kuyrugu ve
    // gunlugu doldurur, arkasindaki saglam mesajlari da yavaslatir.
    public void Basarisiz(string hata, int azamiDeneme, DateTime nowUtc)
    {
        Error = hata;
        AttemptCount++;

        if (AttemptCount >= azamiDeneme)
        {
            DeadLetteredAtUtc = nowUtc;
        }
    }
}
