using FleetOps.SharedKernel.Domain;

namespace FleetOps.Fleet.Domain;

// Filodaki bir otonom tasima aracı. Durum degisiklikleri yalnizca bu sinifin
// metotlari uzerinden yapilir; property'ler disaridan yazilamaz.
public sealed class Agv : AggregateRoot
{
    // Bu esigin altinda AGV'ye yeni gorev verilmez.
    public const int AsgariGorevBataryasi = 20;

    private Agv(Guid id, string code, int batteryLevel) : base(id)
    {
        Code = code;
        BatteryLevel = batteryLevel;
        Status = AgvStatus.Available;
    }

    // EF Core'un nesneyi yeniden olusturabilmesi icin. Elle cagrilmaz.
    private Agv()
    {
        Code = string.Empty;
    }

    public string Code { get; private set; }

    public AgvStatus Status { get; private set; }

    public int BatteryLevel { get; private set; }

    // Stock modulundeki LOCATION kaydinin kimligi. Foreign key DEGIL -
    // moduller arasi FK yoktur, yalnizca ID tasinir.
    public Guid? CurrentLocationId { get; private set; }

    // Optimistic concurrency belirteci. PostgreSQL'in sistem sutunu xmin'e
    // eslenir; ayri bir row_version sutunu tutmaya gerek yok.
    public uint Version { get; private set; }

    public static Result<Agv> Register(Guid id, string code, int batteryLevel)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Agv>(FleetErrors.KodBos);
        }

        if (batteryLevel is < 0 or > 100)
        {
            return Result.Failure<Agv>(FleetErrors.BataryaAraligiDisi);
        }

        return Result.Success(new Agv(id, code.Trim(), batteryLevel));
    }

    // Gorev alabilir mi? Hem durum hem batarya esigi kontrol edilir.
    public bool GorevAlabilir() =>
        Status == AgvStatus.Available && BatteryLevel >= AsgariGorevBataryasi;

    public Result Mesgullestir()
    {
        if (Status == AgvStatus.Busy)
        {
            return Result.Failure(FleetErrors.ZatenMesgul);
        }

        if (!GorevAlabilir())
        {
            return Result.Failure(FleetErrors.GorevAlamaz);
        }

        Status = AgvStatus.Busy;
        return Result.Success();
    }

    // Gorev bitti veya iptal edildi; AGV yeniden musait.
    public void SerbestBirak()
    {
        if (Status == AgvStatus.Busy)
        {
            Status = AgvStatus.Available;
        }
    }

    public Result BataryaBildir(int batteryLevel)
    {
        if (batteryLevel is < 0 or > 100)
        {
            return Result.Failure(FleetErrors.BataryaAraligiDisi);
        }

        BatteryLevel = batteryLevel;
        return Result.Success();
    }

    public void KonumBildir(Guid locationId) => CurrentLocationId = locationId;

    public void SarjaAl() => Status = AgvStatus.Charging;

    public void ServisDisiBirak() => Status = AgvStatus.OutOfService;

    public void ServiseAl() => Status = AgvStatus.Available;
}
