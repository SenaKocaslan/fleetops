using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public static class ResourceErrors
{
    public static readonly Error KodBos =
        new("Resource.KodBos", "Kaynak kodu bos olamaz.");

    public static readonly Error KimlikBos =
        new("Resource.KimlikBos", "Kaynak ve AGV kimligi zorunludur.");

    public static readonly Error SurePozitifOlmali =
        new("Resource.SurePozitifOlmali", "Kilit suresi sifirdan buyuk olmalidir.");

    public static readonly Error Bulunamadi =
        new("Resource.Bulunamadi", "Kaynak bulunamadi.");

    public static readonly Error KilitBulunamadi =
        new("Resource.KilitBulunamadi", "Bu kaynakta aktif kilit yok.");

    public static readonly Error KilitZatenBirakildi =
        new("Resource.KilitZatenBirakildi", "Kilit zaten birakilmis.");

    public static readonly Error KilidiBaskasiTutuyor =
        new("Resource.KilidiBaskasiTutuyor", "Kilidi baska bir AGV tutuyor.");

    public static readonly Error KilidinSuresiDolmadi =
        new("Resource.KilidinSuresiDolmadi", "Kilidin suresi henuz dolmadi.");

    // Kaynak baska bir AGV tarafindan tutuluyor. Istemci yanlis bir sey
    // yapmadi; kaynak serbest kalinca tekrar deneyebilir.
    public static readonly Error KaynakMesgul =
        new("Resource.KaynakMesgul", "Kaynak su anda baska bir AGV tarafindan kilitli.");
}
