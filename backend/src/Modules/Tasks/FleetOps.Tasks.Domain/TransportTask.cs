using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

// Bir malzemenin bir lokasyondan digerine tasinma gorevi.
// Aggregate root: atamalar yalnizca bu sinif uzerinden degistirilir.
public sealed class TransportTask : AggregateRoot
{
    // Durum makinesi TEK yerde. Bir gecis eklenecekse yalnizca bu tablo
    // degisir; kontroller koda dagilmaz.
    private static readonly Dictionary<TransportTaskStatus, TransportTaskStatus[]> IzinliGecisler =
        new()
        {
            [TransportTaskStatus.Pending] = [TransportTaskStatus.Assigned, TransportTaskStatus.Cancelled],
            [TransportTaskStatus.Assigned] = [TransportTaskStatus.InProgress, TransportTaskStatus.Pending],
            [TransportTaskStatus.InProgress] = [TransportTaskStatus.Completed, TransportTaskStatus.Failed],
            [TransportTaskStatus.Completed] = [],
            [TransportTaskStatus.Failed] = [],
            [TransportTaskStatus.Cancelled] = [],
        };

    private readonly List<TaskAssignment> _assignments = [];

    private TransportTask(
        Guid id,
        Guid fromLocationId,
        Guid toLocationId,
        string materialCode,
        int quantity,
        int priority,
        DateTime createdAtUtc) : base(id)
    {
        FromLocationId = fromLocationId;
        ToLocationId = toLocationId;
        MaterialCode = materialCode;
        Quantity = quantity;
        Priority = priority;
        CreatedAtUtc = createdAtUtc;
        Status = TransportTaskStatus.Pending;
    }

    private TransportTask()
    {
        MaterialCode = string.Empty;
    }

    public TransportTaskStatus Status { get; private set; }

    // Stock modulundeki lokasyon kimlikleri. Foreign key DEGIL.
    public Guid FromLocationId { get; private set; }

    public Guid ToLocationId { get; private set; }

    public string MaterialCode { get; private set; }

    public int Quantity { get; private set; }

    // Buyuk sayi daha oncelikli.
    public int Priority { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TaskAssignment> Assignments => _assignments.AsReadOnly();

    // Su an acik olan atama; gorev havuzdaysa null.
    public TaskAssignment? AktifAtama => _assignments.SingleOrDefault(a => a.Aktif);

    // PostgreSQL xmin sistem sutununa eslenir - optimistic concurrency.
    public uint Version { get; private set; }

    public static Result<TransportTask> Create(
        Guid id,
        Guid fromLocationId,
        Guid toLocationId,
        string materialCode,
        int quantity,
        int priority,
        DateTime createdAtUtc)
    {
        if (fromLocationId == Guid.Empty || toLocationId == Guid.Empty)
        {
            return Result.Failure<TransportTask>(TaskErrors.LokasyonBos);
        }

        if (fromLocationId == toLocationId)
        {
            return Result.Failure<TransportTask>(TaskErrors.AyniLokasyon);
        }

        if (string.IsNullOrWhiteSpace(materialCode))
        {
            return Result.Failure<TransportTask>(TaskErrors.MalzemeKoduBos);
        }

        if (quantity <= 0)
        {
            return Result.Failure<TransportTask>(TaskErrors.MiktarPozitifOlmali);
        }

        return Result.Success(new TransportTask(
            id, fromLocationId, toLocationId, materialCode.Trim(), quantity, priority, createdAtUtc));
    }

    public Result Assign(Guid agvId, DateTime nowUtc)
    {
        if (agvId == Guid.Empty)
        {
            return Result.Failure(TaskErrors.AgvBos);
        }

        var gecis = GecisDenetle(TransportTaskStatus.Assigned);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        _assignments.Add(new TaskAssignment(Guid.NewGuid(), Id, agvId, nowUtc));
        Status = TransportTaskStatus.Assigned;
        return Result.Success();
    }

    // AGV gorevi reddetti veya zaman asimina ugradi; gorev havuza doner.
    public Result Release(DateTime nowUtc)
    {
        var gecis = GecisDenetle(TransportTaskStatus.Pending);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        AktifAtama?.Kapat(nowUtc);
        Status = TransportTaskStatus.Pending;
        return Result.Success();
    }

    public Result Start()
    {
        var gecis = GecisDenetle(TransportTaskStatus.InProgress);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        Status = TransportTaskStatus.InProgress;
        return Result.Success();
    }

    public Result Complete(DateTime nowUtc)
    {
        var gecis = GecisDenetle(TransportTaskStatus.Completed);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        AktifAtama?.Kapat(nowUtc);
        Status = TransportTaskStatus.Completed;
        return Result.Success();
    }

    public Result Fail(DateTime nowUtc)
    {
        var gecis = GecisDenetle(TransportTaskStatus.Failed);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        AktifAtama?.Kapat(nowUtc);
        Status = TransportTaskStatus.Failed;
        return Result.Success();
    }

    public Result Cancel()
    {
        var gecis = GecisDenetle(TransportTaskStatus.Cancelled);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        Status = TransportTaskStatus.Cancelled;
        return Result.Success();
    }

    private Result GecisDenetle(TransportTaskStatus hedef) =>
        IzinliGecisler[Status].Contains(hedef)
            ? Result.Success()
            : Result.Failure(TaskErrors.GecersizGecis(Status, hedef));
}
