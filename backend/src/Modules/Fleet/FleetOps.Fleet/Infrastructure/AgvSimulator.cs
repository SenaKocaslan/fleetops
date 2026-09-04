using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetOps.Fleet.Infrastructure;

// Gercek AGV yok. Bu servis her turda her arac icin bir telemetri orneklemi
// uretip normal komut yolundan gecirir; HTTP uc noktasiyla AYNI handler'i
// cagirir, dolayisiyla ayri bir yol acmaz.
public sealed class AgvSimulator(
    IServiceScopeFactory kapsamFabrikasi,
    IOptions<SimulatorOptions> ayarlar,
    ILogger<AgvSimulator> logger) : BackgroundService
{
    private readonly SimulatorOptions _ayarlar = ayarlar.Value;
    private readonly Random _rastgele = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_ayarlar.Enabled)
        {
            return;
        }

        using var zamanlayici = new PeriodicTimer(_ayarlar.Interval);

        while (await zamanlayici.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BirTurCalistirAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Genis catch bilincli: ExecuteAsync'ten disari sizan istisna
                // servisi sessizce durdurur, uygulama ayakta kalmaya devam eder.
                logger.LogError(ex, "Simulator turu basarisiz oldu.");
            }
        }
    }

    // Testler zamanlayiciyi beklemesin diye ayri metot.
    public async Task<int> BirTurCalistirAsync(CancellationToken cancellationToken)
    {
        using var kapsam = kapsamFabrikasi.CreateScope();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var handler = kapsam.ServiceProvider
            .GetRequiredService<ICommandHandler<ReportTelemetryCommand>>();

        var araclar = await db.Agvs
            .AsNoTracking()
            .Select(a => new { a.Id, a.Status, a.BatteryLevel })
            .ToListAsync(cancellationToken);

        var gonderilen = 0;

        foreach (var arac in araclar)
        {
            var komut = new ReportTelemetryCommand(
                arac.Id,
                SonrakiBatarya(arac.Status, arac.BatteryLevel),
                SonrakiKonum());

            var sonuc = await handler.HandleAsync(komut, cancellationToken);

            if (sonuc.IsSuccess)
            {
                gonderilen++;
            }
        }

        return gonderilen;
    }

    // Bosta duran arac harcamaz, calisan harcar, sarjdaki doldurur.
    private int SonrakiBatarya(AgvStatus durum, int batarya) => durum switch
    {
        AgvStatus.Busy => Math.Max(0, batarya - _ayarlar.BatteryDrainPerTick),
        AgvStatus.Charging => Math.Min(100, batarya + _ayarlar.BatteryChargePerTick),
        _ => batarya,
    };

    private Guid? SonrakiKonum() => _ayarlar.LocationIds.Length == 0
        ? null
        : _ayarlar.LocationIds[_rastgele.Next(_ayarlar.LocationIds.Length)];
}
