namespace FleetOps.Fleet.Infrastructure;

public sealed class SimulatorOptions
{
    public const string Bolum = "Simulator";

    // Uretimde gercek AGV'ler telemetri gonderir; simulator yalnizca
    // gelistirme ve test icin acilir. Varsayilan kapali.
    public bool Enabled { get; set; }

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatteryDrainPerTick { get; set; } = 1;

    public int BatteryChargePerTick { get; set; } = 2;

    // Stock modulunun lokasyon id'leri. Fleet, Stock'u referans veremez;
    // modul disi kimlikler koda degil konfigurasyona yazilir.
    public Guid[] LocationIds { get; set; } = [];
}
