using FleetOps.Tasks.Application;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetOps.Tasks.Infrastructure;

// Teslim edilmis outbox mesajlari tabloda birikiyordu (olculdu 2026-09-11:
// 39+ satir, silen bir sey yok). Tablo buyudukce dagiticinin sorgusu da
// yavaslar. Kilit temizleyiciyle ayni kalip.
//
// OLU MEKTUPLARA DOKUNULMAZ. Onlar teslim edilmemis ve insan mudahalesi
// bekleyen kayitlar; silinirse "Tasks.TeslimEdilemeyenOlay" alarmi da
// sessizce kaybolur ve sorun hic yasanmamis gibi gorunur.
public sealed class OutboxTemizleyici(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxTemizleyici> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var zamanlayici = new PeriodicTimer(options.Value.CleanupInterval);

        while (await zamanlayici.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BirTurCalistirAsync(stoppingToken);
            }
            // Yakalanmazsa servis sessizce durur ve tablo yine birikmeye baslar.
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox temizleme turu basarisiz oldu.");
            }
        }
    }

    // Zamanlayicidan ayri: testler turu dogrudan cagirabilsin.
    public async Task<int> BirTurCalistirAsync(CancellationToken cancellationToken)
    {
        await using var kapsam = scopeFactory.CreateAsyncScope();
        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();

        var sinir = DateTime.UtcNow - options.Value.Retention;

        // Tek bir DELETE. Bugunku hacimde parcali silmeye gerek yok; tablo
        // milyonlarca satira ulasirsa uzun sureli kilitten kacinmak icin
        // parcalara bolunmesi gerekir.
        var silinen = await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc != null
                        && m.ProcessedAtUtc < sinir
                        && m.DeadLetteredAtUtc == null)
            .ExecuteDeleteAsync(cancellationToken);

        if (silinen > 0)
        {
            logger.LogInformation("Islenmis {Sayi} outbox mesaji silindi.", silinen);
        }

        return silinen;
    }
}
