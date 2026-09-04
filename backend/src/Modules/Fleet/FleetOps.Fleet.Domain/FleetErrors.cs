using FleetOps.SharedKernel.Domain;

namespace FleetOps.Fleet.Domain;

public static class FleetErrors
{
    public static readonly Error KodBos =
        new("Agv.KodBos", "AGV kodu bos olamaz.");

    public static readonly Error BataryaAraligiDisi =
        new("Agv.BataryaAraligiDisi", "Batarya seviyesi 0-100 araliginda olmalidir.");

    public static readonly Error GorevAlamaz =
        new("Agv.GorevAlamaz", "AGV su anki durumunda gorev alamaz.");

    public static readonly Error ZatenMesgul =
        new("Agv.ZatenMesgul", "AGV zaten mesgul.");

    public static readonly Error Bulunamadi =
        new("Agv.Bulunamadi", "AGV bulunamadi.");

    public static readonly Error EszamanliDegisiklik =
        new("Agv.EszamanliDegisiklik", "AGV bu sirada baska bir islemle degisti.");
}
