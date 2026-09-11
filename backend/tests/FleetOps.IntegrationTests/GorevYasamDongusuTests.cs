using System.Net;
using System.Net.Http.Json;
using FleetOps.Fleet.Domain;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;
using FleetOps.Tasks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FleetOps.IntegrationTests;

// Havuza dondurme, basarisiz ve iptal. Asil sinanan sey modul sinirini gecen
// zincir: Tasks'teki gecis -> domain olayi -> outbox -> Fleet tuketicisi ->
// aracin serbest kalmasi. Zincirin bir halkasi eksik oldugunda arac Fleet'te
// sonsuza kadar Busy kalir ve filodan sessizce duser.
[Collection(VeritabaniKoleksiyonu.Ad)]
public class GorevYasamDongusuTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kabul = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid RafA1 = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    [Fact]
    public async Task Havuza_dondurulen_gorevin_araci_serbest_kalir_ve_yeniden_atanabilir()
    {
        await fabrika.FiloyuHazirlaAsync();
        var istemci = await fabrika.IstemciAsync();
        var gorev = await GorevOlusturAsync(istemci);

        await AtaVeTeslimEtAsync(istemci, gorev);
        Assert.Equal(AgvStatus.Busy, await AgvDurumuAsync());

        var yanit = await istemci.PostAsync($"/api/tasks/{gorev}/release", null);
        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);

        await DagiticiCalistirAsync();

        Assert.Equal(AgvStatus.Available, await AgvDurumuAsync());
        Assert.Equal("Pending", (await GorevAsync(istemci, gorev)).Status);

        // Zincirin sonu: ayni gorev ayni araca tekrar verilebiliyor. Arac
        // Fleet'te Busy kalsaydi uygunluk kontrolu bunu reddederdi.
        var tekrar = await istemci.PostAsJsonAsync($"/api/tasks/{gorev}/assign", new { agvId = Agv01 });
        Assert.Equal(HttpStatusCode.NoContent, tekrar.StatusCode);
    }

    [Fact]
    public async Task Basarisiz_gorevin_araci_serbest_kalir()
    {
        await fabrika.FiloyuHazirlaAsync();
        var istemci = await fabrika.IstemciAsync();
        var gorev = await GorevOlusturAsync(istemci);

        await AtaVeTeslimEtAsync(istemci, gorev);
        await istemci.PostAsync($"/api/tasks/{gorev}/start", null);

        var yanit = await istemci.PostAsync($"/api/tasks/{gorev}/fail", null);
        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);

        await DagiticiCalistirAsync();

        Assert.Equal(AgvStatus.Available, await AgvDurumuAsync());
        Assert.Equal("Failed", (await GorevAsync(istemci, gorev)).Status);
    }

    [Fact]
    public async Task Bekleyen_gorev_iptal_edilir_atanmis_gorev_edilemez()
    {
        await fabrika.FiloyuHazirlaAsync();
        var istemci = await fabrika.IstemciAsync();

        var bekleyen = await GorevOlusturAsync(istemci);
        Assert.Equal(HttpStatusCode.NoContent,
            (await istemci.PostAsync($"/api/tasks/{bekleyen}/cancel", null)).StatusCode);
        Assert.Equal("Cancelled", (await GorevAsync(istemci, bekleyen)).Status);

        var atanmis = await GorevOlusturAsync(istemci);
        await istemci.PostAsJsonAsync($"/api/tasks/{atanmis}/assign", new { agvId = Agv01 });

        var red = await istemci.PostAsync($"/api/tasks/{atanmis}/cancel", null);
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
    }

    [Theory]
    [InlineData("release", HttpStatusCode.Forbidden)]
    [InlineData("cancel", HttpStatusCode.Forbidden)]
    public async Task Operator_planlama_kararini_veremez(string islem, HttpStatusCode beklenen)
    {
        await fabrika.FiloyuHazirlaAsync();
        var gorev = await GorevOlusturAsync(await fabrika.IstemciAsync());
        var operatorIstemci = await fabrika.IstemciAsync(
            FleetOpsApiFactory.OperatorAdi, FleetOpsApiFactory.OperatorParolasi);

        var yanit = await operatorIstemci.PostAsync($"/api/tasks/{gorev}/{islem}", null);

        Assert.Equal(beklenen, yanit.StatusCode);
    }

    [Fact]
    public async Task Operator_yurutulen_gorevi_basarisiz_bildirebilir()
    {
        // Kontrol testi: basarisiz bildirmek yurutmenin parcasi; operator
        // sahada gorevi yuruten kisi.
        await fabrika.FiloyuHazirlaAsync();
        var supervisor = await fabrika.IstemciAsync();
        var gorev = await GorevOlusturAsync(supervisor);
        await AtaVeTeslimEtAsync(supervisor, gorev);
        await supervisor.PostAsync($"/api/tasks/{gorev}/start", null);

        var operatorIstemci = await fabrika.IstemciAsync(
            FleetOpsApiFactory.OperatorAdi, FleetOpsApiFactory.OperatorParolasi);

        var yanit = await operatorIstemci.PostAsync($"/api/tasks/{gorev}/fail", null);

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);
    }

    private async Task AtaVeTeslimEtAsync(HttpClient istemci, Guid gorev)
    {
        var yanit = await istemci.PostAsJsonAsync($"/api/tasks/{gorev}/assign", new { agvId = Agv01 });
        yanit.EnsureSuccessStatusCode();
        await DagiticiCalistirAsync();
    }

    private async Task DagiticiCalistirAsync() =>
        await fabrika.Services.GetServices<IHostedService>()
            .OfType<OutboxDispatcher>().Single()
            .BirTurCalistirAsync(CancellationToken.None);

    private async Task<Guid> GorevOlusturAsync(HttpClient istemci)
    {
        var yanit = await istemci.PostAsJsonAsync(
            "/api/tasks", new CreateTaskCommand(Kabul, RafA1, $"YD-{Guid.NewGuid():N}"[..14], 1, 1));
        yanit.EnsureSuccessStatusCode();
        return (await yanit.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
    }

    private static async Task<TaskSummary> GorevAsync(HttpClient istemci, Guid id)
    {
        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>("/api/tasks?pageSize=100");
        return sayfa!.Items.Single(g => g.Id == id);
    }

    private async Task<AgvStatus> AgvDurumuAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        return (await db.Agvs.AsNoTracking().SingleAsync(a => a.Id == Agv01)).Status;
    }

    private sealed record OlusturmaYaniti(Guid Id);
}
