using FleetOps.SharedKernel.Domain;

namespace FleetOps.UnitTests.SharedKernel;

public class AggregateRootTests
{
    private sealed record TestOldu(DateTime OccurredAtUtc) : IDomainEvent;

    private sealed class TestAggregate : AggregateRoot
    {
        public TestAggregate(Guid id) : base(id)
        {
        }

        public void BirSeyYap() => Raise(new TestOldu(DateTime.UtcNow));
    }

    [Fact]
    public void Bos_kimlikle_olusturulamaz()
    {
        Assert.Throws<ArgumentException>(() => new TestAggregate(Guid.Empty));
    }

    [Fact]
    public void Domain_olaylari_biriktirilir_ve_temizlenebilir()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        Assert.Empty(aggregate.DomainEvents);

        aggregate.BirSeyYap();
        Assert.Single(aggregate.DomainEvents);

        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Esitlik_alanlara_degil_kimlige_gore_belirlenir()
    {
        // Ayni AGV'yi veritabanindan iki kez yuklemek iki C# nesnesi verir
        // ama ayni varliktir.
        var id = Guid.NewGuid();
        Assert.Equal(new TestAggregate(id), new TestAggregate(id));
        Assert.NotEqual(new TestAggregate(id), new TestAggregate(Guid.NewGuid()));
    }
}
