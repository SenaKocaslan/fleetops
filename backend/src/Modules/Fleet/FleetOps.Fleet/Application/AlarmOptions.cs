namespace FleetOps.Fleet.Application;

public sealed class FleetAlarmOptions
{
    public const string Bolum = "FleetAlarms";

    // Gorev esigi (%20) ile ayni degil: arac gorev alamaz hale gelmeden ONCE
    // uyarmak icin daha yuksek.
    public int DusukBataryaEsigi { get; set; } = 30;

    public int KritikBataryaEsigi { get; set; } = 15;

    // Bu sureden uzun sessiz kalan arac ile "hic telemetri gondermemis" arac
    // ayni sey degil; ikincisi henuz devreye alinmamis olabilir.
    public TimeSpan SessizlikSuresi { get; set; } = TimeSpan.FromMinutes(2);
}
