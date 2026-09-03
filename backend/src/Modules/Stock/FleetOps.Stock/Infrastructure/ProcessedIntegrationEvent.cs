namespace FleetOps.Stock.Infrastructure;

// Islenmis olaylarin kaydi. Teslimat en az bir kez oldugu icin ayni olay
// tekrar gelebilir; bu tablo "bunu zaten yaptim" demeyi mumkun kilar.
// Tuketici modulun kendi semasinda durur: idempotentlik tuketicinin
// sorumlulugu, yayinlayanin degil.
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

    // Integration event'in kendi kimligi.
    public Guid Id { get; private set; }

    public DateTime ProcessedAtUtc { get; private set; }
}
