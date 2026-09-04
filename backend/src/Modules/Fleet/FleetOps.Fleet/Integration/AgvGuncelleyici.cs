using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
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

    public static async Task<bool> GuncelleAsync(
        FleetDbContext db,
        IFleetNotifier notifier,
        Guid agvId,
        Func<Agv, bool> degisiklik,
        CancellationToken cancellationToken)
    {
        for (var deneme = 1; ; deneme++)
        {
            var agv = await db.Agvs.FirstOrDefaultAsync(a => a.Id == agvId, cancellationToken);

            if (agv is null)
            {
                return false;
            }

            if (!degisiklik(agv))
            {
                return true;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await notifier.AgvDegistiAsync(AgvSummary.Olustur(agv), cancellationToken);
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
