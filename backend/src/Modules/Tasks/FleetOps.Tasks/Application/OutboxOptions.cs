namespace FleetOps.Tasks.Application;

public sealed class OutboxOptions
{
    public const string Bolum = "Outbox";

    // Daginin veritabanina bakma sikligi.
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    // Bir turda islenecek en fazla mesaj. Sinirsiz olsaydi birikmis bir
    // kuyruk tek turda tum baglanti havuzunu tutardi.
    public int BatchSize { get; set; } = 20;
}
