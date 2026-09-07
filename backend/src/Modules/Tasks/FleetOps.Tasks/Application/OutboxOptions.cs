namespace FleetOps.Tasks.Application;

public sealed class OutboxOptions
{
    public const string Bolum = "Outbox";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int BatchSize { get; set; } = 20;
}
