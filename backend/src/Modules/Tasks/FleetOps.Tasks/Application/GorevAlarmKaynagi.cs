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

        var baslamaSiniri = simdi - _ayarlar.BaslamaEsigi;

        var baslamayanlar = await db.TransportTasks
            .AsNoTracking()
            .Where(g => g.Status == TransportTaskStatus.Assigned
                        && g.Assignments.Any(a => a.CompletedAtUtc == null
                                                  && a.AssignedAtUtc < baslamaSiniri))
            .Select(g => new
            {
                g.MaterialCode,
                AgvId = g.Assignments
                    .Where(a => a.CompletedAtUtc == null)
                    .Select(a => (Guid?)a.AgvId)
                    .FirstOrDefault(),
                AtandiAtUtc = g.Assignments
                    .Where(a => a.CompletedAtUtc == null)
                    .Select(a => (DateTime?)a.AssignedAtUtc)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        foreach (var gorev in baslamayanlar)
        {
            var dakika = gorev.AtandiAtUtc is { } t ? (int)(simdi - t).TotalMinutes : 0;

            alarmlar.Add(new AlarmSummary(
                "Tasks.BaslamayanGorev",
                AlarmSeverity.Kritik,
                gorev.MaterialCode,
                $"Gorev {dakika} dakikadir atanmis ama baslamadi. "
                    + $"Atanan AGV: {gorev.AgvId}. Arac gorevi almamis olabilir.",
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

        // Olu mektup: outbox mesaji azami denemeyi tuketti, olay hic teslim
        // edilmedi. Gunluge yazilmasi yetmez -- kimse gunluge bakmiyor
        // olabilir. Modullerin arasi kalici olarak tutarsiz kaldigi icin
        // bu, insan mudahalesi gerektiren tek alarm turu.
        var oluMektuplar = await db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.DeadLetteredAtUtc != null)
            .OrderBy(m => m.OccurredAtUtc)
            .Select(m => new { m.Id, m.Type, m.AttemptCount, m.Error })
            .ToListAsync(cancellationToken);

        foreach (var mesaj in oluMektuplar)
        {
            alarmlar.Add(new AlarmSummary(
                "Tasks.TeslimEdilemeyenOlay",
                AlarmSeverity.Kritik,
                // Ozne mesajin kendisi, turu degil: ayni turden iki olu
                // mektup varsa ikisi de ayri ayri ele alinmali.
                mesaj.Id.ToString(),
                $"{mesaj.Type} olayi {mesaj.AttemptCount} denemeden sonra teslim "
                    + $"edilemedi ve kuyruktan cikarildi. Son hata: {mesaj.Error}",
                simdi));
        }

        return alarmlar;
    }
}
