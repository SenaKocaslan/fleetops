using FleetOps.SharedKernel.Domain;

namespace FleetOps.UnitTests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Basarili_sonuc_degeri_dondurur()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Basarisiz_sonucun_degeri_okunamaz()
    {
        var result = Result.Failure<int>(new Error("AGV.Mesgul", "AGV musait degil."));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Basarili_sonuc_hata_tasiyamaz()
    {
        // Result'in kendi tutarliligi: basarili + hata birlikte olusturulamaz.
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }
}
