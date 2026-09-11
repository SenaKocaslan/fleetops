using FleetOps.SharedKernel;

namespace FleetOps.Api;

public static class AlarmUcNoktalari
{
    // Bir turden kac ornek gosterilecegi. Amac ekrani okunur tutmak; tam
    // liste icin kaynagin kendisine (gorev listesi, stok ekrani) bakilir.
    public const int OrnekSiniri = 5;

    public static void MapAlarmEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/alarms", async (
            IEnumerable<IAlarmSource> kaynaklar,
            CancellationToken ct) =>
        {
            // Kaynaklar sirayla cagriliyor: her biri kendi DbContext'ini
            // kullaniyor ve tek bir DbContext ayni anda iki sorgu calistiramaz.
            // Farkli context'ler olsa da paralellestirmek olculmus bir problem
            // cozmuyor; kaynak sayisi 3.
            var tumu = new List<AlarmSummary>();

            foreach (var kaynak in kaynaklar)
            {
                tumu.AddRange(await kaynak.AlarmlariGetirAsync(ct));
            }

            // TURE GORE GRUPLAMA. Olculdu (2026-09-11): canli sistemde 51 alarm
            // satirinin 51'i ayni turdendi ("uzun sure bekleyen gorev");
            // operator ayni seyi soyleyen 51 satira bakiyordu. Her turden en
            // fazla OrnekSiniri kadar ornek donuyor, geri kalan sayi olarak.
            //
            // Tur ICINDEKI sira kaynaktan geldigi gibi korunuyor: kaynaklar
            // anlamli siraliyor (ornegin en uzun bekleyen gorev once). Eskiden
            // konuya gore yeniden siralaniyordu ve bu bilgi kayboluyordu.
            var gruplar = tumu
                .GroupBy(a => (a.Code, a.Severity))
                .OrderByDescending(g => g.Key.Severity)
                .ThenBy(g => g.Key.Code, StringComparer.Ordinal)
                .Select(g => new
                {
                    Kod = g.Key.Code,
                    Siddet = g.Key.Severity,
                    Adet = g.Count(),
                    Ornekler = g.Take(OrnekSiniri).ToList(),
                })
                .ToList();

            var ornekler = gruplar
                .SelectMany(g => g.Ornekler)
                .Select(a => new
                {
                    code = a.Code,
                    severity = a.Severity.ToString(),
                    subject = a.Subject,
                    message = a.Message,
                    detectedAtUtc = a.DetectedAtUtc,
                })
                .ToList();

            return Results.Ok(new
            {
                items = ornekler,
                groups = gruplar.Select(g => new
                {
                    code = g.Kod,
                    severity = g.Siddet.ToString(),
                    count = g.Adet,
                    shown = g.Ornekler.Count,
                }),
                totalCount = tumu.Count,
                // Rozet KESILMEDEN hesaplaniyor: gosterilen ornek sayisi degil,
                // gercek kritik alarm sayisi.
                criticalCount = tumu.Count(a => a.Severity == AlarmSeverity.Kritik),
            });
        }).WithTags("Alarms").RequireAuthorization(Politikalar.Okuma);
    }
}
