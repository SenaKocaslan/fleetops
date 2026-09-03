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
        var yanit = await fabrika.CreateClient().GetAsync("/api/agvs");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var agvler = await yanit.Content.ReadFromJsonAsync<List<AgvSummary>>();

        // Baska testler kendi AGV'lerini olusturabiliyor; iddia listenin
        // tamami degil, tohumlananlarin icinde bulunmasi.
        var kodlar = agvler!.Select(a => a.Code).ToList();
        Assert.Contains("AGV-01", kodlar);
        Assert.Contains("AGV-02", kodlar);
        Assert.Contains("AGV-03", kodlar);
    }

    [Fact]
    public async Task Gorev_alabilirlik_domain_kuralina_gore_hesaplanir()
    {
        var agvler = await fabrika.CreateClient()
            .GetFromJsonAsync<List<AgvSummary>>("/api/agvs");

        // Sabit bir AGV'nin durumuna bagli assert yazilmiyor: integration
        // event'ler artik AGV durumunu degistiriyor ve baska testler bunu
        // tetikleyebilir. Iddia kuralin KENDISI: gorev alabilirlik, musait
        // olmak ve batarya esigini gecmekle ayni sey.
        Assert.NotEmpty(agvler!);
        Assert.All(agvler!, a =>
            Assert.Equal(a.Status == "Available" && a.BatteryLevel >= 20, a.GorevAlabilir));

        // AGV-03 sarjda ve batarya esigin altinda; onu hicbir test degistirmiyor.
        var sarjdaki = Assert.Single(agvler!, a => a.Code == "AGV-03");
        Assert.False(sarjdaki.GorevAlabilir);
    }
}
