using FleetOps.Tasks.Domain;

namespace FleetOps.UnitTests.Tasks;

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

        Assert.Equal(agvId, olay.AgvId);
        Assert.Equal("MLZ-100", olay.MaterialCode);
        Assert.Equal(4, olay.Quantity);
        Assert.Equal(gorev.FromLocationId, olay.FromLocationId);
    }

    [Fact]
    public void Basarisiz_gecis_olay_yaymaz()
    {
        var gorev = Gorev();

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

    [Fact]
    public void Havuza_dondurme_aracin_kimligiyle_atama_bitti_olayini_yayar()
    {
        var gorev = Gorev();
        var agvId = Guid.NewGuid();
        gorev.Assign(agvId, Simdi);

        gorev.Release(Simdi.AddMinutes(3));

        // Kimlik atama KAPANMADAN once okunmali; sonra okunsaydi bos gelirdi
        // ve Fleet hangi araci serbest birakacagini bilemezdi.
        var olay = Assert.Single(gorev.DomainEvents.OfType<TaskAssignmentEndedDomainEvent>());
        Assert.Equal(agvId, olay.AgvId);
        Assert.Equal(AtamaBitisSebebi.HavuzaDondu, olay.Sebep);
        Assert.Null(gorev.AktifAtama);
        Assert.Equal(TransportTaskStatus.Pending, gorev.Status);
    }

    [Fact]
    public void Basarisizlik_aracin_kimligiyle_atama_bitti_olayini_yayar()
    {
        var gorev = Gorev();
        var agvId = Guid.NewGuid();
        gorev.Assign(agvId, Simdi);
        gorev.Start();

        gorev.Fail(Simdi.AddMinutes(8));

        var olay = Assert.Single(gorev.DomainEvents.OfType<TaskAssignmentEndedDomainEvent>());
        Assert.Equal(agvId, olay.AgvId);
        Assert.Equal(AtamaBitisSebebi.Basarisiz, olay.Sebep);
    }

    [Fact]
    public void Iptal_olay_yaymaz_cunku_serbest_birakilacak_arac_yok()
    {
        var gorev = Gorev();

        Assert.True(gorev.Cancel().IsSuccess);

        Assert.Empty(gorev.DomainEvents.OfType<TaskAssignmentEndedDomainEvent>());
    }

    [Fact]
    public void Atanmis_gorev_dogrudan_iptal_edilemez()
    {
        // Once havuza donmeli: aksi halde arac hic serbest birakilmazdi.
        var gorev = Gorev();
        gorev.Assign(Guid.NewGuid(), Simdi);

        Assert.True(gorev.Cancel().IsFailure);
    }
}
