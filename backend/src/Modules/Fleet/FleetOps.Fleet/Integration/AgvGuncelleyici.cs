using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Infrastructure;
using FleetOps.Fleet.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Fleet.Integration;

// Telemetri her arac icin birkac saniyede bir AGV satirini guncelliyor.
// Integration event handler'lari bu yarisi kaybedince olay teslim edilemiyor
// ve outbox'ta hata ile bekliyor (olculdu 2026-09-04: outbox_message.error =
// "expected to affect 1 row(s), but actually affected 0 row(s)").
//
// Burada yeniden deneme DOGRU, telemetride yanlisti: telemetri tekrar eden bir
// olcum, kaybedilen ornegin yerini bir sonraki alir. Durum degisikligi ise tek
// seferlik bir olay; kaybedilirse kimse tekrarlamaz.
public static class AgvGuncelleyici
{
    private const int AzamiDeneme = 3;

    // olayId verilirse (integration event tuketicileri) olay BIR KEZ islenir.
    // Olculdu (2026-09-11): teslimat en az bir kez; onceki gorevin tekrar gelen
    // bitis olayi araci yeni gorevinin ortasinda serbest birakti. Ayni sekilde
    // tekrar gelen bir atama olayi, gorevi bitmis araci yeniden mesgul yapip
    // orada takili birakirdi. Stock modulundeki kalibin aynisi: "islendi"
    // isareti durum degisikligiyle AYNI SaveChanges'te yaziliyor.
    public static async Task<bool> GuncelleAsync(
        FleetDbContext db,
        IFleetNotifier notifier,
        Guid agvId,
        Func<Agv, bool> degisiklik,
        CancellationToken cancellationToken,
        Guid? olayId = null)
    {
        if (olayId is { } id)
        {
            // Optimizasyon, kuralin kendisi degil: asil bekci birincil anahtar.
            if (await db.ProcessedEvents.AnyAsync(e => e.Id == id, cancellationToken))
            {
                return true;
            }

            // Dongunun DISINDA bir kez: xmin catismasinda yeniden denenirken
            // ikinci kez eklenirse ayni anahtarli iki nesne izlenirdi.
            db.ProcessedEvents.Add(new ProcessedIntegrationEvent(id, DateTime.UtcNow));
        }

        for (var deneme = 1; ; deneme++)
        {
            var agv = await db.Agvs.FirstOrDefaultAsync(a => a.Id == agvId, cancellationToken);

            if (agv is null)
            {
                // Olay islendi sayilir: bu arac icin yapilacak bir sey yok.
                if (olayId is not null)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                return false;
            }

            var degisti = degisiklik(agv);

            // Degisiklik olmasa da olay isaretlenmeli. Ornek: arac sarjdayken
            // gelen atama olayi bir sey degistirmez; isaretlenmezse arac sonra
            // musait oldugunda tekrar teslim edilen ayni olay onu, belki coktan
            // havuza donmus bir gorev icin mesgul yapardi.
            if (!degisti && olayId is null)
            {
                return true;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);

                if (degisti)
                {
                    await notifier.AgvDegistiAsync(AgvSummary.Olustur(agv), cancellationToken);
                }

                return true;
            }
            catch (DbUpdateConcurrencyException) when (deneme < AzamiDeneme)
            {
                // Takipteki nesne eski surumu tutuyor; ayirmazsak sonraki
                // deneme de ayni xmin ile gider ve sonsuza kadar catisir.
                db.Entry(agv).State = EntityState.Detached;
            }
        }
    }
}
