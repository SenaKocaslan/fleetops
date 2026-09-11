using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public static class ResourceErrors
{
    public static readonly Error KodBos =
        new("Resource.KodBos", "Kaynak kodu boş olamaz.");

    public static readonly Error KimlikBos =
        new("Resource.KimlikBos", "Kaynak ve AGV kimliği zorunludur.");

    public static readonly Error SurePozitifOlmali =
        new("Resource.SurePozitifOlmali", "Kilit süresi sıfırdan büyük olmalıdır.");

    public static readonly Error Bulunamadi =
        new("Resource.Bulunamadi", "Kaynak bulunamadı.");

    public static readonly Error KilitBulunamadi =
        new("Resource.KilitBulunamadi", "Bu kaynakta aktif kilit yok.");

    public static readonly Error KilitZatenBirakildi =
        new("Resource.KilitZatenBirakildi", "Kilit zaten bırakılmış.");

    public static readonly Error KilidiBaskasiTutuyor =
        new("Resource.KilidiBaskasiTutuyor", "Kilidi başka bir AGV tutuyor.");

    public static readonly Error KilidinSuresiDolmadi =
        new("Resource.KilidinSuresiDolmadi", "Kilidin süresi henüz dolmadı.");

    // Gorev yurutmekte olan (Busy) arac kilit ALABILIR -- kilit zaten gorev
    // sirasinda alinir. Alamayan, sahada olmayan arac: sarjdaki ya da servis
    // disi. Oyle bir arac koridoru kilit suresi dolana kadar bosuna kapatirdi.
    public static Error AgvSahadaDegil(string kod) =>
        new("Resource.AgvSahadaDegil", $"{kod} sahada çalışmıyor; kaynak kilidi alamaz.");

    public static readonly Error AgvBulunamadi =
        new("Resource.AgvBulunamadi", "Kilidi alacak AGV bulunamadı.");

    public static readonly Error KaynakMesgul =
        new("Resource.KaynakMesgul", "Kaynak şu anda başka bir AGV tarafından kilitli.");
}
