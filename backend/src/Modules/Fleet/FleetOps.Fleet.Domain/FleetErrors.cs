using FleetOps.SharedKernel.Domain;

namespace FleetOps.Fleet.Domain;

public static class FleetErrors
{
    public static readonly Error KodBos =
        new("Agv.KodBos", "AGV kodu boş olamaz.");

    public static readonly Error BataryaAraligiDisi =
        new("Agv.BataryaAraligiDisi", "Batarya seviyesi 0–100 aralığında olmalıdır.");

    public static readonly Error GorevAlamaz =
        new("Agv.GorevAlamaz", "AGV şu anki durumunda görev alamaz.");

    public static readonly Error ZatenMesgul =
        new("Agv.ZatenMesgul", "AGV zaten meşgul.");

    public static readonly Error Bulunamadi =
        new("Agv.Bulunamadi", "AGV bulunamadı.");

    public static readonly Error EszamanliDegisiklik =
        new("Agv.EszamanliDegisiklik", "AGV bu sırada başka bir işlemle değişti.");
}
