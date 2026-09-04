using FleetOps.SharedKernel;
using FleetOps.Tasks.Domain;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FleetOps.Tasks.Application;

internal sealed class GorevAlarmKaynagi(
    TasksDbContext db,
    IOptions<TasksAlarmOptions> ayarlar) : IAlarmSource
{
    private readonly TasksAlarmOptions _ayarlar = ayarlar.Value;

    public async Task<IReadOnlyList<AlarmSummary>> AlarmlariGetirAsync(
        CancellationToken cancellationToken)
    {
        var simdi = DateTime.UtcNow;
        var alarmlar = new List<AlarmSummary>();

        var bekleyenSinir = simdi - _ayarlar.BeklemeEsigi;

        var bekleyenler = await db.TransportTasks
            .AsNoTracking()
            .Where(g => g.Status == TransportTaskStatus.Pending && g.CreatedAtUtc < bekleyenSinir)
            .OrderBy(g => g.CreatedAtUtc)
            .Select(g => new { g.MaterialCode, g.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        foreach (var gorev in bekleyenler)
        {
            alarmlar.Add(new AlarmSummary(
                "Tasks.UzunSureBekleyenGorev",
                AlarmSeverity.Uyari,
                gorev.MaterialCode,
                $"Gorev {(int)(simdi - gorev.CreatedAtUtc).TotalMinutes} dakikadir atanmadi.",
                simdi));
        }

        // Suresi dolmus AMA hala aktif kilit: LockReaper calismiyor demektir.
        // Tolerans, reaper'in bir sonraki turunu beklemek icin.
        var kilitSiniri = simdi - _ayarlar.KilitGecikmeToleransi;

        var takiliKilitler = await db.ResourceLocks
            .AsNoTracking()
            .Where(k => k.ReleasedAtUtc == null && k.ExpiresAtUtc < kilitSiniri)
            .Select(k => new { k.ResourceId, k.AgvId, k.ExpiresAtUtc })
            .ToListAsync(cancellationToken);

        foreach (var kilit in takiliKilitler)
        {
            alarmlar.Add(new AlarmSummary(
                "Tasks.TakiliKilit",
                AlarmSeverity.Kritik,
                kilit.ResourceId.ToString(),
                $"Kilit {kilit.ExpiresAtUtc:HH:mm:ss} itibariyla dolmus ama hala aktif. "
                    + $"Tutan AGV: {kilit.AgvId}.",
                simdi));
        }

        return alarmlar;
    }
}
