using FleetOps.SharedKernel;

namespace FleetOps.Api;

public static class AlarmUcNoktalari
{
    public static void MapAlarmEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/alarms", async (
            IEnumerable<IAlarmSource> kaynaklar,
            CancellationToken ct) =>
        {
            // Kaynaklar sirayla cagriliyor: her biri kendi DbContext'ini
            // kullaniyor ve tek bir DbContext ayni anda iki sorgu calistiramaz.
            // Farkli context'ler olsa da paralellestirmek olculmus bir problem
            // cozmuyor; kaynak sayisi 2.
            var tumu = new List<AlarmSummary>();

            foreach (var kaynak in kaynaklar)
            {
                tumu.AddRange(await kaynak.AlarmlariGetirAsync(ct));
            }

            var sirali = tumu
                .OrderByDescending(a => a.Severity)
                .ThenBy(a => a.Code)
                .ThenBy(a => a.Subject)
                .Select(a => new
                {
                    code = a.Code,
                    severity = a.Severity.ToString(),
                    subject = a.Subject,
                    message = a.Message,
                    detectedAtUtc = a.DetectedAtUtc,
                })
                .ToList();

            return Results.Ok(new { items = sirali, criticalCount = tumu.Count(a => a.Severity == AlarmSeverity.Kritik) });
        }).WithTags("Alarms").RequireAuthorization(Politikalar.Okuma);
    }
}
