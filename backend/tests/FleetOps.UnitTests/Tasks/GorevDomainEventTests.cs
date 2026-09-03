using FleetOps.Tasks.Domain;

namespace FleetOps.UnitTests.Tasks;

// Aggregate'in dis dunyaya haber verecegi anlari isaretledigi yer.
// Bu olaylar disari cikmadan once integration event'e cevriliyor.
public class GorevDomainEventTests
{
    private static readonly DateTime Simdi = new(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);

    private static TransportTask Gorev() =>
        TransportTask.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "MLZ-100", 4, 1, Simdi).Value;

    [Fact]
    public void Atama_TaskAssigned_olayi_yayar()
    {
        var gorev = Gorev();
        var agvId = Guid.NewGuid();

        gorev.Assign(agvId, Simdi);

        var olay = Assert.Single(gorev.DomainEvents.OfType<TaskAssignedDomainEvent>());
        Assert.Equal(gorev.Id, olay.TaskId);
        Assert.Equal(agvId, olay.AgvId);
    }

    [Fact]
    public void Tamamlama_TaskCompleted_olayini_atamayi_kapatmadan_once_doldurur()
    {
        var gorev = Gorev();
        var agvId = Guid.NewGuid();
        gorev.Assign(agvId, Simdi);
        gorev.Start();

        gorev.Complete(Simdi.AddMinutes(10));

        var olay = Assert.Single(gorev.DomainEvents.OfType<TaskCompletedDomainEvent>());

        // AGV kimligi atama kapandiktan sonra okunsaydi bos gelirdi.
        Assert.Equal(agvId, olay.AgvId);
        Assert.Equal("MLZ-100", olay.MaterialCode);
        Assert.Equal(4, olay.Quantity);
        Assert.Equal(gorev.FromLocationId, olay.FromLocationId);
    }

    [Fact]
    public void Basarisiz_gecis_olay_yaymaz()
    {
        var gorev = Gorev();

        // Pending'den dogrudan Completed'a gecilemez.
        var sonuc = gorev.Complete(Simdi);

        Assert.True(sonuc.IsFailure);
        Assert.Empty(gorev.DomainEvents);
    }

    [Fact]
    public void Temizlenen_olaylar_ikinci_kez_okunmaz()
    {
        var gorev = Gorev();
        gorev.Assign(Guid.NewGuid(), Simdi);

        gorev.ClearDomainEvents();

        Assert.Empty(gorev.DomainEvents);
    }
}
