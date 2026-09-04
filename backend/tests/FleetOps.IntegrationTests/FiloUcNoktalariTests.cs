using System.Net;
using System.Net.Http.Json;
using FleetOps.Fleet.Application;
using FleetOps.IntegrationTests.Altyapi;

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
        var agvler = await (await fabrika.IstemciAsync())
            .GetFromJsonAsync<List<AgvSummary>>("/api/agvs");

        Assert.NotEmpty(agvler!);
        // Sabit bir AGV durumuna bagli assert yazma: integration event'ler
        // AGV durumunu degistiriyor.
        Assert.All(agvler!, a =>
            Assert.Equal(a.Status == "Available" && a.BatteryLevel >= 20, a.GorevAlabilir));

        var sarjdaki = Assert.Single(agvler!, a => a.Code == "AGV-03");
        Assert.False(sarjdaki.GorevAlabilir);
    }
}
