using FleetOps.Fleet.Domain;

namespace FleetOps.UnitTests.Fleet;

public class AgvTests
{
    private static Agv Musait(int batarya = 100) =>
        Agv.Register(Guid.NewGuid(), "AGV-01", batarya).Value;

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_kodla_kaydedilemez(string kod)
    {
        var sonuc = Agv.Register(Guid.NewGuid(), kod, 100);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(FleetErrors.KodBos, sonuc.Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Batarya_araligi_disinda_kaydedilemez(int batarya)
    {
        var sonuc = Agv.Register(Guid.NewGuid(), "AGV-01", batarya);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(FleetErrors.BataryaAraligiDisi, sonuc.Error);
    }

    [Fact]
    public void Yeni_agv_musait_baslar()
    {
        Assert.Equal(AgvStatus.Available, Musait().Status);
    }

    [Fact]
    public void Batarya_esigin_altindaysa_gorev_alamaz()
    {
        var agv = Musait(Agv.AsgariGorevBataryasi - 1);

        Assert.False(agv.GorevAlabilir());
        Assert.Equal(FleetErrors.GorevAlamaz, agv.Mesgullestir().Error);
    }

    [Fact]
    public void Esik_degerinde_gorev_alabilir()
    {
        // Sinir degeri: esigin TAM ustunde de calismali (off-by-one korumasi).
        Assert.True(Musait(Agv.AsgariGorevBataryasi).GorevAlabilir());
    }

    [Fact]
    public void Sarj_olurken_gorev_alamaz()
    {
        var agv = Musait();
        agv.SarjaAl();

        Assert.False(agv.GorevAlabilir());
        Assert.True(agv.Mesgullestir().IsFailure);
    }

    [Fact]
    public void Ikinci_kez_mesgullestirilemez()
    {
        var agv = Musait();
        Assert.True(agv.Mesgullestir().IsSuccess);

        var ikinci = agv.Mesgullestir();

        Assert.True(ikinci.IsFailure);
        Assert.Equal(FleetErrors.ZatenMesgul, ikinci.Error);
    }

    [Fact]
    public void Serbest_birakilinca_yeniden_musait_olur()
    {
        var agv = Musait();
        agv.Mesgullestir();

        agv.SerbestBirak();

        Assert.Equal(AgvStatus.Available, agv.Status);
    }
}
