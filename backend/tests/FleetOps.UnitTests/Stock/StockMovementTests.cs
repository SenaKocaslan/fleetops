using FleetOps.Stock.Domain;

namespace FleetOps.UnitTests.Stock;

public class StockMovementTests
{
    private static readonly DateTime Simdi = new(2026, 9, 6, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Kaynak = Guid.NewGuid();
    private static readonly Guid Hedef = Guid.NewGuid();

    [Fact]
    public void Gecerli_hareket_olusturulur()
    {
        var gorevId = Guid.NewGuid();

        var sonuc = StockMovement.Create(
            Guid.NewGuid(), "  MLZ-100  ", 5, Kaynak, Hedef, gorevId, Simdi);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("MLZ-100", sonuc.Value.MaterialCode);
        Assert.Equal(gorevId, sonuc.Value.SourceTaskId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Miktar_pozitif_olmali(int miktar)
    {
        var sonuc = StockMovement.Create(
            Guid.NewGuid(), "MLZ-100", miktar, Kaynak, Hedef, Guid.NewGuid(), Simdi);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(StockErrors.MiktarPozitifOlmali, sonuc.Error);
    }

    [Fact]
    public void Kaynak_ve_hedef_ayni_olamaz()
    {
        var sonuc = StockMovement.Create(
            Guid.NewGuid(), "MLZ-100", 1, Kaynak, Kaynak, Guid.NewGuid(), Simdi);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(StockErrors.AyniLokasyon, sonuc.Error);
    }

    [Fact]
    public void Malzeme_kodu_bos_olamaz()
    {
        var sonuc = StockMovement.Create(
            Guid.NewGuid(), "   ", 1, Kaynak, Hedef, Guid.NewGuid(), Simdi);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(StockErrors.MalzemeKoduBos, sonuc.Error);
    }

    [Fact]
    public void Lokasyon_kodu_bos_olamaz()
    {
        var sonuc = Location.Create(Guid.NewGuid(), " ", "Depo");

        Assert.True(sonuc.IsFailure);
        Assert.Equal(StockErrors.LokasyonKoduBos, sonuc.Error);
    }
}
