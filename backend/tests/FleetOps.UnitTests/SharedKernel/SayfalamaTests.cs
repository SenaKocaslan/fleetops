using FleetOps.SharedKernel.Domain;

namespace FleetOps.UnitTests.SharedKernel;

public class PageRequestTests
{
    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Sayfa_numarasi_en_az_bir_olur(int? gelen, int beklenen)
    {
        Assert.Equal(beklenen, new PageRequest(gelen, null).Page);
    }

    [Theory]
    [InlineData(null, PageRequest.VarsayilanBoyut)]
    [InlineData(0, PageRequest.VarsayilanBoyut)]
    [InlineData(-1, PageRequest.VarsayilanBoyut)]
    [InlineData(50, 50)]
    public void Sayfa_boyutu_gecersizse_varsayilana_duser(int? gelen, int beklenen)
    {
        Assert.Equal(beklenen, new PageRequest(null, gelen).PageSize);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(1_000_000)]
    public void Sayfa_boyutu_ustten_sinirlanir(int gelen)
    {
        // Sinirsiz pageSize tek istekle tum tabloyu bellege cekmenin yolu.
        Assert.Equal(PageRequest.AzamiBoyut, new PageRequest(null, gelen).PageSize);
    }

    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(3, 15, 30)]
    public void Atlanacak_kayit_sayisi_dogru_hesaplanir(int sayfa, int boyut, int beklenen)
    {
        Assert.Equal(beklenen, new PageRequest(sayfa, boyut).Atlanacak);
    }
}

public class PagedResultTests
{
    private static PagedResult<int> Sonuc(int sayfa, int boyut, int toplam) =>
        new([], sayfa, boyut, toplam);

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(40, 20, 2)]
    [InlineData(41, 20, 3)]
    public void Toplam_sayfa_yukari_yuvarlanir(int toplam, int boyut, int beklenen)
    {
        Assert.Equal(beklenen, Sonuc(1, boyut, toplam).TotalPages);
    }

    [Fact]
    public void Son_sayfada_sonraki_sayfa_yoktur()
    {
        Assert.False(Sonuc(2, 20, 40).HasNext);
        Assert.True(Sonuc(1, 20, 40).HasNext);
    }

    [Fact]
    public void Bos_sonucta_sonraki_sayfa_yoktur()
    {
        Assert.False(Sonuc(1, 20, 0).HasNext);
    }
}
