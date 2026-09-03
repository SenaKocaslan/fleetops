using FleetOps.SharedKernel;
using FleetOps.Tasks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetOps.Tasks.Infrastructure;

// Suresi dolmus kilitleri periyodik olarak serbest birakir.
// Bu servis olmadan takilan bir AGV'nin kilidi sonsuza kadar kalir ve
// kaynak bir daha kimseye verilemez.
public sealed class LockReaper(
    IServiceScopeFactory scopeFactory,
    IOptions<ResourceLockOptions> options,
    ILogger<LockReaper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var zamanlayici = new PeriodicTimer(options.Value.ReaperInterval);

        while (await zamanlayici.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BirTurCalistirAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Yakalamazsak BackgroundService sessizce durur ve kimse
                // fark etmez; uygulama calisiyor gorunur ama kilitler
                // bir daha hic temizlenmez.
                logger.LogError(ex, "Kilit temizleme turu basarisiz oldu.");
            }
        }
    }

    // Zamanlayicidan bagimsiz tek tur. Testten dogrudan cagrilabilsin diye
    // ayri: zamanlayiciyi beklemek testi hem yavas hem flaky yapardi.
    public async Task<int> BirTurCalistirAsync(CancellationToken cancellationToken)
    {
        // Handler'lar scoped; barindirilan servis singleton. Her tur icin
        // kendi kapsamini acmak zorunda.
        await using var kapsam = scopeFactory.CreateAsyncScope();

        var handler = kapsam.ServiceProvider
            .GetRequiredService<ICommandHandler<ReapExpiredLocksCommand, int>>();

        var sonuc = await handler.HandleAsync(new ReapExpiredLocksCommand(), cancellationToken);

        if (sonuc.Value > 0)
        {
            logger.LogInformation(
                "Suresi dolan {Sayi} kilit serbest birakildi.", sonuc.Value);
        }

        return sonuc.Value;
    }
}
