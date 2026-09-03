namespace FleetOps.Tasks.Application;

// Kilit suresi ve temizleme araligi koda gomulmez: ortama gore degisir.
// Yogun bir tesiste kilit suresi kisa, yavas AGV'lerde uzun olmali.
public sealed class ResourceLockOptions
{
    public const string Bolum = "ResourceLock";

    // Bir kilit alindiginda ne kadar sure gecerli olacak.
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);

    // Suresi dolmus kilitleri temizleyen servisin calisma araligi.
    public TimeSpan ReaperInterval { get; set; } = TimeSpan.FromSeconds(30);
}
