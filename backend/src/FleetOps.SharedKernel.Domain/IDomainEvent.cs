namespace FleetOps.SharedKernel.Domain;

/// <summary>
/// Modul ICINDE olan bir olay. Ayni transaction'da islenir.
/// Moduller ARASI iletisim icin kullanilmaz - onun icin integration event vardir.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
