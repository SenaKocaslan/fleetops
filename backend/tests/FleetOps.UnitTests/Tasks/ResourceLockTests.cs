using FleetOps.Tasks.Domain;

namespace FleetOps.UnitTests.Tasks;

public class ResourceLockTests
{
    private static readonly DateTime Simdi = new(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Kaynak = Guid.NewGuid();
    private static readonly Guid Agv = Guid.NewGuid();

    private static ResourceLock Kilit(TimeSpan? sure = null) =>
        ResourceLock.Acquire(Guid.NewGuid(), Kaynak, Agv, Simdi, sure ?? TimeSpan.FromMinutes(5)).Value;

    [Fact]
    public void Alinan_kilit_aktiftir_ve_bitis_zamani_hesaplanir()
    {
        var kilit = Kilit(TimeSpan.FromMinutes(5));

        Assert.True(kilit.Aktif);
        Assert.Equal(Simdi.AddMinutes(5), kilit.ExpiresAtUtc);
        Assert.False(kilit.SuresiDoldu(Simdi.AddMinutes(4)));
        Assert.True(kilit.SuresiDoldu(Simdi.AddMinutes(5)));
    }

    [Fact]
    public void Sifir_veya_negatif_sure_reddedilir()
    {
        var sonuc = ResourceLock.Acquire(Guid.NewGuid(), Kaynak, Agv, Simdi, TimeSpan.Zero);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ResourceErrors.SurePozitifOlmali, sonuc.Error);
    }

    [Fact]
    public void Bos_kimlikle_kilit_alinamaz()
    {
        var sonuc = ResourceLock.Acquire(Guid.NewGuid(), Kaynak, Guid.Empty, Simdi, TimeSpan.FromMinutes(1));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ResourceErrors.KimlikBos, sonuc.Error);
    }

    [Fact]
    public void Kilidi_tutan_agv_birakabilir()
    {
        var kilit = Kilit();

        var sonuc = kilit.Release(Agv, Simdi.AddMinutes(1));

        Assert.True(sonuc.IsSuccess);
        Assert.False(kilit.Aktif);
        Assert.Equal(Simdi.AddMinutes(1), kilit.ReleasedAtUtc);
    }

    [Fact]
    public void Baska_agv_kilidi_birakamaz()
    {
        var kilit = Kilit();

        var sonuc = kilit.Release(Guid.NewGuid(), Simdi.AddMinutes(1));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ResourceErrors.KilidiBaskasiTutuyor, sonuc.Error);
        Assert.True(kilit.Aktif);
    }

    [Fact]
    public void Birakilan_kilit_ikinci_kez_birakilamaz()
    {
        var kilit = Kilit();
        kilit.Release(Agv, Simdi.AddMinutes(1));

        var sonuc = kilit.Release(Agv, Simdi.AddMinutes(2));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ResourceErrors.KilitZatenBirakildi, sonuc.Error);
    }

    [Fact]
    public void Suresi_dolmayan_kilit_zaman_asimiyla_birakilamaz()
    {
        var kilit = Kilit(TimeSpan.FromMinutes(5));

        var sonuc = kilit.ZamanAsimiylaBirak(Simdi.AddMinutes(4));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ResourceErrors.KilidinSuresiDolmadi, sonuc.Error);
        Assert.True(kilit.Aktif);
    }

    [Fact]
    public void Suresi_dolan_kilit_sahibi_sorulmadan_birakilir()
    {
        var kilit = Kilit(TimeSpan.FromMinutes(5));

        var sonuc = kilit.ZamanAsimiylaBirak(Simdi.AddMinutes(5));

        Assert.True(sonuc.IsSuccess);
        Assert.False(kilit.Aktif);
    }

    [Fact]
    public void Kodu_bos_kaynak_olusturulamaz()
    {
        var sonuc = Resource.Create(Guid.NewGuid(), "   ", ResourceKind.Corridor);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ResourceErrors.KodBos, sonuc.Error);
    }
}
