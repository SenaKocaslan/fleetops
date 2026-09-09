using System.Net;
using System.Net.Http.Json;
using FleetOps.Fleet.Application;
using FleetOps.Fleet.Persistence;
using FleetOps.IntegrationTests.Altyapi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class FiloUcNoktalariTests(FleetOpsApiFactory fabrika)
{
    [Fact]
    public async Task Tohumlanan_agvler_listelenir()
    {
        var yanit = await (await fabrika.IstemciAsync()).GetAsync("/api/agvs");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var agvler = await yanit.Content.ReadFromJsonAsync<List<AgvSummary>>();

        var kodlar = agvler!.Select(a => a.Code).ToList();
        Assert.Contains("AGV-01", kodlar);
        Assert.Contains("AGV-02", kodlar);
        Assert.Contains("AGV-03", kodlar);
    }

    [Fact]
    public async Task Gorev_alabilirlik_domain_kuralina_gore_hesaplanir()
    {
        // Tohum verideki duruma bel baglamak yerine test kendi sahnesini
        // kuruyor: baska testler araclarin durumunu degistirebiliyor.
        await Agv03SarjaAlAsync();

        var agvler = await (await fabrika.IstemciAsync())
            .GetFromJsonAsync<List<AgvSummary>>("/api/agvs");

        Assert.NotEmpty(agvler!);
        // Sabit bir AGV durumuna bagli assert yazma: integration event'ler
        // AGV durumunu degistiriyor.
        Assert.All(agvler!, a =>
            Assert.Equal(a.Status == "Available" && a.BatteryLevel >= 20, a.GorevAlabilir));

        var sarjdaki = Assert.Single(agvler!, a => a.Code == "AGV-03");
        Assert.Equal("Charging", sarjdaki.Status);
        Assert.False(sarjdaki.GorevAlabilir);
    }

    private async Task Agv03SarjaAlAsync()
    {
        using var kapsam = fabrika.KapsamAc();
        var db = kapsam.ServiceProvider.GetRequiredService<FleetDbContext>();
        var agv = await db.Agvs.SingleAsync(
            a => a.Id == Guid.Parse("33333333-3333-3333-3333-333333333333"));
        agv.SarjaAl();
        await db.SaveChangesAsync();
    }
}
