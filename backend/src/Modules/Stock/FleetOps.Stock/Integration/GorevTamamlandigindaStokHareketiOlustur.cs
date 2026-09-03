using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
using FleetOps.Stock.Domain;
using FleetOps.Stock.Infrastructure;
using FleetOps.Stock.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetOps.Stock.Integration;

// Gorev tamamlaninca stok hareketi olusur.
//
// Bu tuketici IDEMPOTENT OLMAK ZORUNDA. Fleet'teki "AGV'yi serbest birak"
// tekrarlanabilir bir islem, ama "hareket kaydi olustur" degil: ayni olay
// iki kez gelirse iki kayit olusur ve depo sayimlari bozulur.
internal sealed class GorevTamamlandigindaStokHareketiOlustur(
    StockDbContext db,
    ILogger<GorevTamamlandigindaStokHareketiOlustur> logger)
    : IntegrationEventHandler<TaskCompletedIntegrationEvent>
{
    protected override async Task HandleAsync(
        TaskCompletedIntegrationEvent olay,
        CancellationToken cancellationToken)
    {
        // Bu kontrol bir OPTIMIZASYON, kuralin kendisi degil: asil bekci
        // processed_integration_event tablosunun birincil anahtari. Iki
        // teslim ayni anda gelirse ikisi de bu kontrolu gecebilir, ama
        // ikinci INSERT anahtar cakismasindan doner ve transaction geri
        // alinir. Kontrol yalnizca bilinen tekrarlarda gereksiz exception
        // uretmemek icin var. (Bilerek bozup dogrulandi: tek basina bu
        // kontrolu kaldirmak cift kayit olusturmuyor.)
        var islenmis = await db.ProcessedEvents
            .AnyAsync(e => e.Id == olay.Id, cancellationToken);

        if (islenmis)
        {
            return;
        }

        var hareket = StockMovement.Create(
            Guid.NewGuid(),
            olay.MaterialCode,
            olay.Quantity,
            olay.FromLocationId,
            olay.ToLocationId,
            olay.TaskId,
            olay.OccurredAtUtc);

        if (hareket.IsFailure)
        {
            // Olayin icerigi bir daha degismeyecek; tekrar denemek ayni
            // sonucu verir ve kuyrugu sonsuza kadar tikar. Islenmis
            // isaretleyip hatayi kayda geciyoruz.
            logger.LogError(
                "Stok hareketi olusturulamadi ({Kod}): {Mesaj}",
                hareket.Error.Code, hareket.Error.Message);
        }
        else
        {
            db.StockMovements.Add(hareket.Value);
        }

        db.ProcessedEvents.Add(new ProcessedIntegrationEvent(olay.Id, DateTime.UtcNow));

        // KRITIK: hareket ile "islendi" isareti AYNI transaction'da yazilir.
        // Ayri yazilsaydi arada cakan bir hata ya cift kayda ya da hic
        // islenmemis gorunen bir olaya yol acardi.
        await db.SaveChangesAsync(cancellationToken);
    }
}
