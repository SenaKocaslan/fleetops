using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

// Bir AGV'nin bir kaynak uzerindeki kilidi. Suresi doludur: AGV takilirsa
// kilit sonsuza kadar kalmasin. Birakilan kilit silinmez, kaydi kalir -
// "bu kaynagi kim ne zaman tuttu" sorusu cevaplanabilsin.
public sealed class ResourceLock : AggregateRoot
{
    private ResourceLock(
        Guid id,
        Guid resourceId,
        Guid agvId,
        DateTime acquiredAtUtc,
        DateTime expiresAtUtc) : base(id)
    {
        ResourceId = resourceId;
        AgvId = agvId;
        AcquiredAtUtc = acquiredAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    private ResourceLock()
    {
    }

    public Guid ResourceId { get; private set; }

    public Guid AgvId { get; private set; }

    public DateTime AcquiredAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? ReleasedAtUtc { get; private set; }

    public uint Version { get; private set; }

    public bool Aktif => ReleasedAtUtc is null;

    public bool SuresiDoldu(DateTime nowUtc) => ExpiresAtUtc <= nowUtc;

    public static Result<ResourceLock> Acquire(
        Guid id,
        Guid resourceId,
        Guid agvId,
        DateTime nowUtc,
        TimeSpan sure)
    {
        if (resourceId == Guid.Empty || agvId == Guid.Empty)
        {
            return Result.Failure<ResourceLock>(ResourceErrors.KimlikBos);
        }

        if (sure <= TimeSpan.Zero)
        {
            return Result.Failure<ResourceLock>(ResourceErrors.SurePozitifOlmali);
        }

        return Result.Success(new ResourceLock(id, resourceId, agvId, nowUtc, nowUtc + sure));
    }

    // Kilidi yalnizca tutan AGV birakabilir.
    public Result Release(Guid agvId, DateTime nowUtc)
    {
        if (!Aktif)
        {
            return Result.Failure(ResourceErrors.KilitZatenBirakildi);
        }

        if (agvId != AgvId)
        {
            return Result.Failure(ResourceErrors.KilidiBaskasiTutuyor);
        }

        ReleasedAtUtc = nowUtc;
        return Result.Success();
    }

    // Zaman asimiyla birakma: sahibi kontrol edilmez, cunku bunu sistem
    // yapiyor. Ama suresi dolmamis kilide dokunulmaz.
    public Result ZamanAsimiylaBirak(DateTime nowUtc)
    {
        if (!Aktif)
        {
            return Result.Failure(ResourceErrors.KilitZatenBirakildi);
        }

        if (!SuresiDoldu(nowUtc))
        {
            return Result.Failure(ResourceErrors.KilidinSuresiDolmadi);
        }

        ReleasedAtUtc = nowUtc;
        return Result.Success();
    }
}
