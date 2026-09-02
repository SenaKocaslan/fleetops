using FleetOps.SharedKernel.Domain;

namespace FleetOps.Fleet.Domain;

// Beklenen is hatalari tek yerde. Kod string'leri API yanitinda ve
// istemcide kullanilacagi icin dagilmamalari onemli.
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
}
