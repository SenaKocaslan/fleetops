namespace FleetOps.SharedKernel.IntegrationEvents;

// Tasks -> Stock ve Fleet. Bir gorev tamamlandi: malzeme tasindi ve AGV
// serbest kaldi. Stock'un ihtiyaci olan tum alanlar olayin icinde tasinir;
// tuketici kaynak modulun veritabanina donup sormaz.
public sealed record TaskCompletedIntegrationEvent(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid TaskId,
    Guid AgvId,
    string MaterialCode,
    int Quantity,
    Guid FromLocationId,
    Guid ToLocationId) : IntegrationEvent(Id, OccurredAtUtc);
