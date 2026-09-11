using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel;
using FleetOps.SharedKernel.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

// Outbox teslimati EN AZ BIR KEZ: ayni olay iki kez gelebilir (dagitici
// tuketiciyi calistirdiktan sonra "islendi" isaretini yazamazsa bir sonraki
// turda tekrar dener). Tuketiciler idempotent olmak zorunda. Bu testler
// olaylari dogrudan handler'lara, dagiticinin yaptigi sekilde teslim ediyor.
[Collection(VeritabaniKoleksiyonu.Ad)]
public class TekrarTeslimTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kabul = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid RafA1 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    [Fact]
    public async Task Onceki_gorevin_tekrar_gelen_bitis_olayi_araci_yeni_gorevinde_serbest_birakmaz()
    {
        await fabrika.FiloyuHazirlaAsync();
        var gorevA = Guid.NewGuid();
        var gorevB = Guid.NewGuid();

        var tamamlandiA = new TaskCompletedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, gorevA, Agv01, "MLZ-TEKRAR", 1, Kabul, RafA1);

        await TeslimEtAsync(new TaskAssignedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, gorevA, Agv01));
        await TeslimEtAsync(tamamlandiA);
        Assert.Equal(AgvStatus.Available, await DurumAsync());

        await TeslimEtAsync(new TaskAssignedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, gorevB, Agv01));
        Assert.Equal(AgvStatus.Busy, await DurumAsync());

        // A'nin bitis olayi AYNEN tekrar geliyor. Arac simdi B'yi yurutuyor;
        // serbest birakilirsa otomatik atama ona ucuncu bir gorev verir ve
        // B yarida kalir.
        await TeslimEtAsync(tamamlandiA);

        Assert.Equal(AgvStatus.Busy, await DurumAsync());
    }

    [Fact]
    public async Task Bitmis_gorevin_tekrar_gelen_atama_olayi_bostaki_araci_mesgul_yapmaz()
    {
        await fabrika.FiloyuHazirlaAsync();
        var gorevA = Guid.NewGuid();

        var atandiA = new TaskAssignedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, gorevA, Agv01);

        await TeslimEtAsync(atandiA);
        await TeslimEtAsync(new TaskCompletedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, gorevA, Agv01, "MLZ-TEKRAR", 1, Kabul, RafA1));
        Assert.Equal(AgvStatus.Available, await DurumAsync());

        // A'nin atama olayi tekrar geliyor. Isaretlenmeseydi arac, coktan
        // bitmis bir gorev icin mesgul olur ve orada takili kalirdi: o
        // gorevin bir daha bitis olayi gelmeyecek.
        await TeslimEtAsync(atandiA);

        Assert.Equal(AgvStatus.Available, await DurumAsync());
    }

    private async Task TeslimEtAsync(IntegrationEvent olay)
    {
        using var kapsam = fabrika.KapsamAc();
        foreach (var tuketici in kapsam.ServiceProvider.GetServices<IIntegrationEventHandler>()
                     .Where(t => t.EventType == olay.GetType()))
        {
            await tuketici.HandleAsync(olay, CancellationToken.None);
        }
    }

    private async Task<AgvStatus> DurumAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        return (await db.Agvs.AsNoTracking().SingleAsync(a => a.Id == Agv01)).Status;
    }
}
