using FleetOps.Fleet.Application;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Integration;
using FleetOps.Fleet.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetOps.Fleet.Infrastructure;

// NEDEN TELEMETRIDE DEGIL: telemetri aracin bildirimi, bir karar degil.
// "Bataryasi dusen arac sarja gitsin" bir filo POLITIKASI ve zamanla
// degisebilir (gece sarj, sirayla sarj, en yakin istasyon). Telemetriye
// gomulseydi her olcum bir karar noktasi olurdu ve arac kendi durumunu
// kendi degistirir hale gelirdi. Bu servis kilit temizleyiciyle ayni
// kaliptа: periyodik bakar, gereken durum gecisini komut olarak uygular.
public sealed class SarjYonlendirici(
    IServiceScopeFactory kapsamFabrikasi,
    IOptions<SarjOptions> ayarlar,
    ILogger<SarjYonlendirici> logger) : BackgroundService
{
    private readonly SarjOptions _ayarlar = ayarlar.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var zamanlayici = new PeriodicTimer(_ayarlar.Interval);

        while (await zamanlayici.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BirTurCalistirAsync(stoppingToken);
            }
            // Yakalanmazsa servis sessizce durur ve hicbir arac bir daha
            // sarja gitmez; filo yavasca tukenir.
            catch (Exception ex)
            {
                logger.LogError(ex, "Sarj yonlendirme turu basarisiz oldu.");
            }
        }
    }

    // Zamanlayicidan ayri: testler turu dogrudan cagirabilsin.
    public async Task<(int SarjaGiden, int ServiseDonen)> BirTurCalistirAsync(
        CancellationToken cancellationToken)
    {
        await using var kapsam = kapsamFabrikasi.CreateAsyncScope();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var notifier = kapsam.ServiceProvider.GetRequiredService<IFleetNotifier>();

        // MESGUL ARAC SARJA GONDERILMEZ. Yurutulen gorev yarida kalirdi ve
        // gorev havuza donmedigi icin kimse fark etmezdi. Arac gorevi
        // bitirip Available'a dondugunde bir sonraki tur onu alir.
        var sarjaGidecekler = await db.Agvs
            .AsNoTracking()
            .Where(a => a.Status == AgvStatus.Available
                        && a.BatteryLevel < _ayarlar.SarjaGonderEsigi)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var servisDonecekler = await db.Agvs
            .AsNoTracking()
            .Where(a => a.Status == AgvStatus.Charging
                        && a.BatteryLevel >= _ayarlar.SarjdanDonEsigi)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var sarjaGiden = 0;
        var serviseDonen = 0;

        foreach (var id in sarjaGidecekler)
        {
            // Yeniden okuyup guncelliyoruz: yukaridaki liste ile bu satir
            // arasinda arac gorev almis olabilir. AgvGuncelleyici xmin
            // catismasini da ele aliyor.
            if (await Uygula(db, notifier, id, SarjaAlKarari, cancellationToken))
            {
                sarjaGiden++;
            }
        }

        foreach (var id in servisDonecekler)
        {
            if (await Uygula(db, notifier, id, ServiseAlKarari, cancellationToken))
            {
                serviseDonen++;
            }
        }

        if (sarjaGiden > 0 || serviseDonen > 0)
        {
            logger.LogInformation(
                "Sarj yonlendirme: {SarjaGiden} arac sarja alindi, {ServiseDonen} arac servise dondu.",
                sarjaGiden, serviseDonen);
        }

        return (sarjaGiden, serviseDonen);
    }

    private bool SarjaAlKarari(Agv agv)
    {
        if (agv.Status != AgvStatus.Available || agv.BatteryLevel >= _ayarlar.SarjaGonderEsigi)
        {
            return false;
        }

        agv.SarjaAl();
        return true;
    }

    private bool ServiseAlKarari(Agv agv)
    {
        if (agv.Status != AgvStatus.Charging || agv.BatteryLevel < _ayarlar.SarjdanDonEsigi)
        {
            return false;
        }

        agv.ServiseAl();
        return true;
    }

    private static async Task<bool> Uygula(
        FleetDbContext db,
        IFleetNotifier notifier,
        Guid id,
        Func<Agv, bool> karar,
        CancellationToken cancellationToken)
    {
        var degisti = false;

        await AgvGuncelleyici.GuncelleAsync(db, notifier, id, agv =>
        {
            degisti = karar(agv);
            return degisti;
        }, cancellationToken);

        return degisti;
    }
}
