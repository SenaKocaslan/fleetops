using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

// Domain event ile integration event ayni sey degil:
// - Domain event modulun ICINDE kalir, zengin olabilir ve serbestce
//   degisebilir; kimse ona bagimli degildir.
// - Integration event modul DISINA cikan bir sozlesmedir; degistirmek
//   baska modulleri kirar.
// Bu yuzden aggregate domain event yayar, disari cikarken cevrilir.
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
