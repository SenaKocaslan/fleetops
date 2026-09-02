namespace FleetOps.SharedKernel.Domain;

/// <summary>
/// Bir tutarlilik sinirinin giris kapisi. Aggregate icindeki nesneler
/// yalnizca root uzerinden degistirilir; boylece gecersiz ara durum olusamaz.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>Kaydedildikten sonra yayinlanmayi bekleyen olaylar.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Olaylar yayinlandiktan sonra altyapi tarafindan temizlenir.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
