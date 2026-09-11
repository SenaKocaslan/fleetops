using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public sealed class TransportTask : AggregateRoot
{
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

    // Durum makinesinden TURETILIYOR, elle yazilmiyor: cikisi olmayan durum
    // bitmis demektir. Ayri bir liste tutulsaydi yeni bir bitis durumu
    // eklendiginde listeye eklemeyi unutmak mumkun olurdu ve siralama
    // sessizce yanlis calisirdi.
    public static readonly TransportTaskStatus[] BitmisDurumlar =
        [.. IzinliGecisler.Where(g => g.Value.Length == 0).Select(g => g.Key).Order()];

    // Buyuk sayi daha oncelikli. Alt sinir olmadiginda 0 ve negatif degerler
    // kabul ediliyordu; oyle bir gorev listenin dibine duser, otomatik atama
    // onu en son alir, yani sessizce gorunmez olur. Ust sinir olmadiginda da
    // "siranin onune gecmek" icin 1000 yazmak mumkundu -- olculdu: bu
    // projenin kendi testlerinde 99, 900 ve 1000 degerleri tam bu sebeple
    // birikmisti. Gercek kullanimdaki degerler 1, 3, 5 ve 9.
    public const int AsgariOncelik = 1;
    public const int AzamiOncelik = 10;

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

    public Guid FromLocationId { get; private set; }

    public Guid ToLocationId { get; private set; }

    public string MaterialCode { get; private set; }

    public int Quantity { get; private set; }

    public int Priority { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TaskAssignment> Assignments => _assignments.AsReadOnly();

    public TaskAssignment? AktifAtama => _assignments.SingleOrDefault(a => a.Aktif);

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

        if (priority is < AsgariOncelik or > AzamiOncelik)
        {
            return Result.Failure<TransportTask>(TaskErrors.OncelikAraligiDisi);
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
        Raise(new TaskAssignedDomainEvent(Id, agvId, nowUtc));
        return Result.Success();
    }

    public Result Release(DateTime nowUtc)
    {
        var gecis = GecisDenetle(TransportTaskStatus.Pending);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        AtamayiBitir(nowUtc, AtamaBitisSebebi.HavuzaDondu);
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

        // Atama kapanmadan once okunmali; sonra okunursa bos gelir.
        var agvId = AktifAtama?.AgvId ?? Guid.Empty;

        AktifAtama?.Kapat(nowUtc);
        Status = TransportTaskStatus.Completed;

        Raise(new TaskCompletedDomainEvent(
            Id, agvId, MaterialCode, Quantity, FromLocationId, ToLocationId, nowUtc));

        return Result.Success();
    }

    public Result Fail(DateTime nowUtc)
    {
        var gecis = GecisDenetle(TransportTaskStatus.Failed);
        if (gecis.IsFailure)
        {
            return gecis;
        }

        AtamayiBitir(nowUtc, AtamaBitisSebebi.Basarisiz);
        Status = TransportTaskStatus.Failed;
        return Result.Success();
    }

    // Arac kimligi atama KAPANMADAN once okunmali; sonra okunursa bos gelir.
    private void AtamayiBitir(DateTime nowUtc, string sebep)
    {
        if (AktifAtama is not { } atama)
        {
            return;
        }

        var agvId = atama.AgvId;
        atama.Kapat(nowUtc);
        Raise(new TaskAssignmentEndedDomainEvent(Id, agvId, sebep, nowUtc));
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
