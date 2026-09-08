namespace FleetOps.Tasks.Application;

public sealed class OutboxOptions
{
    public const string Bolum = "Outbox";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int BatchSize { get; set; } = 20;

    // Bu kadar denemeden sonra mesaj olu mektuba tasinir. Dusuk olursa
    // gecici bir kesinti kalici kayba donusur; yuksek olursa bozuk mesaj
    // kuyrugu uzun sure mesgul eder.
    public int MaxAttempts { get; set; } = 5;
}
