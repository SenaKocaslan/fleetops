namespace FleetOps.Tasks.Application;

public sealed class ResourceLockOptions
{
    public const string Bolum = "ResourceLock";

    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan ReaperInterval { get; set; } = TimeSpan.FromSeconds(30);
}
