namespace FleetOps.SharedKernel.Domain;

/// <summary>
/// Kimligi olan domain nesnesi. Esitlik, alanlara degil kimlige gore belirlenir:
/// veritabanindan iki kez yuklenen ayni AGV, iki farkli C# nesnesi olsa da
/// ayni varliktir.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity kimligi bos olamaz.", nameof(id));
        }

        Id = id;
    }

    /// <summary>ORM'in nesneyi yeniden olusturabilmesi icin. Elle cagrilmaz.</summary>
    protected Entity()
    {
    }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
