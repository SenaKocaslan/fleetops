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

// TESLIMAT EN AZ BIR KEZ: tuketici baska modulun veritabanina yaziyor, yani
// outbox satirini "islendi" isaretlemekle ayni transaction'da degil. Yeni
// tuketici yazan herkes idempotent yazmak zorunda.
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
            // Yakalanmazsa servis sessizce durur ve olaylar bir daha teslim edilmez.
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dagitim turu basarisiz oldu.");
            }
        }
    }

    public async Task<int> BirTurCalistirAsync(CancellationToken cancellationToken)
    {
        await using var kapsam = scopeFactory.CreateAsyncScope();

        var db = kapsam.ServiceProvider.GetRequiredService<TasksDbContext>();
        var kayitDefteri = kapsam.ServiceProvider.GetRequiredService<IIntegrationEventTypeRegistry>();

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
                mesaj.Basarisiz(HataMetni(ex));
                logger.LogError(ex, "Outbox mesaji islenemedi: {MesajId}", mesaj.Id);
            }
        }

        if (mesajlar.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return islenen;
    }

    // ex.Message tek basina yetmiyor: EF'in "An error occurred while saving
    // the entity changes" mesaji asil sebebi ic istisnada birakiyor ve
    // outbox tablosuna bakan kisi hicbir sey ogrenemiyor.
    private static string HataMetni(Exception ex)
    {
        var parcalar = new List<string>();

        for (Exception? mevcut = ex; mevcut is not null; mevcut = mevcut.InnerException)
        {
            parcalar.Add($"{mevcut.GetType().Name}: {mevcut.Message}");
        }

        var metin = string.Join(" -> ", parcalar);
        return metin.Length > 2000 ? metin[..2000] : metin;
    }
}
