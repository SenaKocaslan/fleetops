using FleetOps.SharedKernel;
using FleetOps.Tasks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetOps.Tasks.Infrastructure;

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
            // Yakalanmazsa BackgroundService sessizce durur ve kilitler bir daha
            // hic temizlenmez.
            catch (Exception ex)
            {
                logger.LogError(ex, "Kilit temizleme turu basarisiz oldu.");
            }
        }
    }

    // Zamanlayicidan ayri: handler'lar scoped, bu servis singleton.
    public async Task<int> BirTurCalistirAsync(CancellationToken cancellationToken)
    {
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
