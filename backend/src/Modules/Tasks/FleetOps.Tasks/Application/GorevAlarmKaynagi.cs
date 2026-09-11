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
                $"Görev {SureMetni(simdi - gorev.CreatedAtUtc)} atanmadı.",
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
            var bekleme = gorev.AtandiAtUtc is { } t ? simdi - t : TimeSpan.Zero;

            alarmlar.Add(new AlarmSummary(
                "Tasks.BaslamayanGorev",
                AlarmSeverity.Kritik,
                gorev.MaterialCode,
                $"Görev {SureMetni(bekleme)} atanmış ama başlamadı. "
                    + $"Atanan AGV: {gorev.AgvId}. Araç görevi almamış olabilir; "
                    + "görev havuza döndürülebilir.",
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
                $"Kilit {kilit.ExpiresAtUtc:HH:mm:ss} itibarıyla dolmuş ama hâlâ aktif. "
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
                $"{mesaj.Type} olayı {mesaj.AttemptCount} denemeden sonra teslim "
                    + $"edilemedi ve kuyruktan çıkarıldı. Son hata: {mesaj.Error}",
                simdi));
        }

        return alarmlar;
    }

    // "4533 dakikadir" dogru ama okunmuyor. Sure okunur birime cevriliyor.
    // Ekler sabit kelimelere bagli oldugu icin elle yazildi (dakika-dir,
    // saat-tir, gun-dur).
    internal static string SureMetni(TimeSpan sure) => sure switch
    {
        { TotalMinutes: < 60 } => $"{(int)sure.TotalMinutes} dakikadır",
        { TotalHours: < 24 } => $"{(int)sure.TotalHours} saattir",
        _ => $"{(int)sure.TotalDays} gündür",
    };
}
