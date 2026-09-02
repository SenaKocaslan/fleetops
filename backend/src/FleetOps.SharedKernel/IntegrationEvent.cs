namespace FleetOps.SharedKernel;

/// <summary>
/// Moduller ARASI olay. Outbox tablosuna yazilir, arka plan isleyicisi
/// tarafindan yayinlanir. Domain event'ten farki: domain event modul
/// icinde ve ayni transaction'da, bu ise moduller arasi ve gecikmelidir.
/// Tuketici idempotent olmak zorundadir - ayni olay iki kez gelebilir.
/// </summary>
public abstract record IntegrationEvent(Guid Id, DateTime OccurredAtUtc);
