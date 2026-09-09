namespace FleetOps.Fleet.Infrastructure;

public sealed class SarjOptions
{
    public const string Bolum = "Sarj";

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);

    // IKI ESIK, tek esik DEGIL. Tek esik olsaydi tam o degerdeki bir arac
    // her turda sarja gir/cik yapardi: sarja alinir, bir tik sarj olur,
    // esigi gecer, servise alinir, gorev alir, tekrar duser. Aradaki bosluk
    // (histerezis) bu salinimi kesiyor.
    public int SarjaGonderEsigi { get; set; } = 25;

    public int SarjdanDonEsigi { get; set; } = 80;
}
