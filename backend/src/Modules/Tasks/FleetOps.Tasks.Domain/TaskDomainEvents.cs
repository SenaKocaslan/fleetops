using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public sealed record TaskAssignedDomainEvent(
    Guid TaskId,
    Guid AgvId,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record TaskCompletedDomainEvent(
    Guid TaskId,
    Guid AgvId,
    string MaterialCode,
    int Quantity,
    Guid FromLocationId,
    Guid ToLocationId,
    DateTime OccurredAtUtc) : IDomainEvent;

// Gorev bitmeden atama sona erdi: havuza donduruldu ya da basarisiz oldu.
// Ikisinde de arac artik bu gorevde degil ve Fleet onu serbest birakmali.
// Olmadan arac Fleet'te sonsuza kadar Busy kalir: tamamlanma olayi hic
// gelmeyecek, sarj yonlendirici de mesgul araca dokunmuyor. Arac filodan
// sessizce duser.
public sealed record TaskAssignmentEndedDomainEvent(
    Guid TaskId,
    Guid AgvId,
    string Sebep,
    DateTime OccurredAtUtc) : IDomainEvent;

public static class AtamaBitisSebebi
{
    public const string HavuzaDondu = "HavuzaDondu";
    public const string Basarisiz = "Basarisiz";
}
