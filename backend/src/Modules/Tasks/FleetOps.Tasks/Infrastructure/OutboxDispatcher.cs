using System.Text.Json;
using FleetOps.SharedKernel;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetOps.Tasks.Infrastructure;

// Islenmemis outbox satirlarini okur ve kayitli tuketicilere teslim eder.
//
// TESLIMAT EN AZ BIR KEZ (at-least-once). Tuketici baska bir modulun
// veritabanina yaziyor, yani outbox satirini "islendi" isaretlemekle ayni
// transaction'da degil. Tuketici calistiktan sonra isaretleme basarisiz
// olursa ayni olay tekrar teslim edilir. Bu yuzden tuketicinin idempotent
// olmasi zorunlu - bu bir tercih degil, mekanizmanin sonucu.
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var zamanlayici = new PeriodicTimer(options.Value.PollInterval);

        while (await zamanlayici.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BirTurCalistirAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Yakalanmazsa servis sessizce durur ve olaylar bir daha
                // hic teslim edilmez.
                logger.LogError(ex, "Outbox dagitim turu basarisiz oldu.");
            }
        }
    }

    // Zamanlayicidan bagimsiz tek tur; testten dogrudan cagrilabilir.
    // Islenen mesaj sayisini doner.
    public async Task<int> BirTurCalistirAsync(CancellationToken cancellationToken)
    {
        await using var kapsam = scopeFactory.CreateAsyncScope();

        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        var kayitDefteri = kapsam.ServiceProvider.GetRequiredService<IIntegrationEventTypeRegistry>();

        // Tuketiciler baska modullerde; Tasks onlarin turunu bilmez,
        // yalnizca ortak arayuzu gorur.
        var tuketiciler = kapsam.ServiceProvider.GetServices<IIntegrationEventHandler>().ToList();

        var mesajlar = await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        var islenen = 0;

        foreach (var mesaj in mesajlar)
        {
            try
            {
                var tur = kayitDefteri.Cozumle(mesaj.Type)
                    ?? throw new InvalidOperationException(
                        $"'{mesaj.Type}' turu cozumlenemedi. Olay sozlesmesi kaldirilmis olabilir.");

                var olay = (IntegrationEvent)JsonSerializer.Deserialize(mesaj.Payload, tur)!;

                foreach (var tuketici in tuketiciler.Where(t => t.EventType == tur))
                {
                    await tuketici.HandleAsync(olay, cancellationToken);
                }

                mesaj.Islendi(DateTime.UtcNow);
                islenen++;
            }
            catch (Exception ex)
            {
                // Islenmis isaretlenmiyor: bir sonraki turda tekrar denenecek.
                mesaj.Basarisiz(ex.Message);
                logger.LogError(ex, "Outbox mesaji islenemedi: {MesajId}", mesaj.Id);
            }
        }

        if (mesajlar.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return islenen;
    }
}
