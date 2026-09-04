using FleetOps.SharedKernel.Domain;

namespace FleetOps.Fleet.Domain;

public sealed class Agv : AggregateRoot
{
    public const int AsgariGorevBataryasi = 20;

    private Agv(Guid id, string code, int batteryLevel) : base(id)
    {
        Code = code;
        BatteryLevel = batteryLevel;
        Status = AgvStatus.Available;
    }

    private Agv()
    {
        Code = string.Empty;
    }

    public string Code { get; private set; }

    public AgvStatus Status { get; private set; }

    public int BatteryLevel { get; private set; }

    public Guid? CurrentLocationId { get; private set; }

    public DateTime? LastSeenAtUtc { get; private set; }

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

    // Telemetri Status'u DEGISTIRMEZ. Durum gecisleri atama/sarj komutlarina ait;
    // arac yalnizca olctugunu bildirir. GorevAlabilir() zaten bataryaya baktigi
    // icin dusen batarya araci kendiliginden atanamaz hale getirir.
    public Result TelemetriBildir(int batteryLevel, Guid? locationId, DateTime nowUtc)
    {
        var batarya = BataryaBildir(batteryLevel);
        if (batarya.IsFailure)
        {
            return batarya;
        }

        if (locationId is { } konum)
        {
            KonumBildir(konum);
        }

        LastSeenAtUtc = nowUtc;
        return Result.Success();
    }

    public void SarjaAl() => Status = AgvStatus.Charging;

    public void ServisDisiBirak() => Status = AgvStatus.OutOfService;

    public void ServiseAl() => Status = AgvStatus.Available;
}
