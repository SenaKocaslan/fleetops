using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FleetOps.Fleet.Application;

internal sealed class FiloAlarmKaynagi(
    FleetDbContext db,
    IOptions<FleetAlarmOptions> ayarlar) : IAlarmSource
{
    private readonly FleetAlarmOptions _ayarlar = ayarlar.Value;

    public async Task<IReadOnlyList<AlarmSummary>> AlarmlariGetirAsync(
        CancellationToken cancellationToken)
    {
        var simdi = DateTime.UtcNow;

        var araclar = await db.Agvs
            .AsNoTracking()
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);

        var alarmlar = new List<AlarmSummary>();

        foreach (var arac in araclar)
        {
            if (arac.BatteryLevel <= _ayarlar.KritikBataryaEsigi)
            {
                alarmlar.Add(new AlarmSummary(
                    "Fleet.KritikBatarya",
                    AlarmSeverity.Kritik,
                    arac.Code,
                    $"Batarya %{arac.BatteryLevel}. Arac sarja alinmali.",
                    simdi));
            }
            else if (arac.BatteryLevel <= _ayarlar.DusukBataryaEsigi)
            {
                alarmlar.Add(new AlarmSummary(
                    "Fleet.DusukBatarya",
                    AlarmSeverity.Uyari,
                    arac.Code,
                    $"Batarya %{arac.BatteryLevel}, esik %{_ayarlar.DusukBataryaEsigi}.",
                    simdi));
            }

            // Hic telemetri gelmemis arac "sessiz" degil, "devreye alinmamis".
            // Ikisini ayirmazsak sistem her yeni araca alarm uretir.
            if (arac.LastSeenAtUtc is { } sonGorulme &&
                simdi - sonGorulme > _ayarlar.SessizlikSuresi)
            {
                alarmlar.Add(new AlarmSummary(
                    "Fleet.TelemetriKesildi",
                    AlarmSeverity.Kritik,
                    arac.Code,
                    $"Son telemetri {(int)(simdi - sonGorulme).TotalSeconds} saniye once.",
                    simdi));
            }
        }

        return alarmlar;
    }
}
