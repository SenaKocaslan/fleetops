using System.Net;
using System.Net.Http.Json;
using FleetOps.IntegrationTests.Altyapi;
using FleetOps.SharedKernel.Domain;
using FleetOps.Tasks.Application;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class GorevUcNoktalariTests(FleetOpsApiFactory fabrika)
{
    private static CreateTaskCommand GecerliIstek(string malzeme = "MLZ-100", int miktar = 5) =>
        new(Guid.NewGuid(), Guid.NewGuid(), malzeme, miktar, 1);

    [Fact]
    public async Task Gorev_listesi_bos_da_olsa_200_doner()
    {
        var yanit = await (await fabrika.IstemciAsync()).GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var sayfa = await yanit.Content.ReadFromJsonAsync<PagedResult<TaskSummary>>();
        Assert.NotNull(sayfa);
        Assert.Equal(1, sayfa.Page);
        Assert.Equal(PageRequest.VarsayilanBoyut, sayfa.PageSize);
    }

    [Fact]
    public async Task Olusturulan_gorev_listede_gorunur()
    {
        var istemci = await fabrika.IstemciAsync();
        var malzeme = $"MLZ-{Guid.NewGuid().ToString()[..8]}";

        var olustur = await istemci.PostAsJsonAsync("/api/tasks", GecerliIstek(malzeme, 7));

        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);

        var sayfa = await istemci.GetFromJsonAsync<PagedResult<TaskSummary>>(
            "/api/tasks?pageSize=100");
        var liste = sayfa!.Items;
        var gorev = Assert.Single(liste, g => g.MaterialCode == malzeme);

        Assert.Equal("Pending", gorev.Status);
        Assert.Equal(7, gorev.Quantity);
        Assert.Null(gorev.AssignedAgvId);
    }

    [Fact]
    public async Task Gecersiz_gorev_400_ve_hata_kodu_doner()
    {
        var istemci = await fabrika.IstemciAsync();
        var lokasyon = Guid.NewGuid();

        var yanit = await istemci.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskCommand(lokasyon, lokasyon, "MLZ-100", 5, 1));

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);

        var hata = await yanit.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Task.AyniLokasyon", hata?.Code);
    }

    [Fact]
    public async Task Miktar_sifir_ise_reddedilir()
    {
        var yanit = await (await fabrika.IstemciAsync())
            .PostAsJsonAsync("/api/tasks", GecerliIstek(miktar: 0));

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
        var hata = await yanit.Content.ReadFromJsonAsync<HataYaniti>();
        Assert.Equal("Task.MiktarPozitifOlmali", hata?.Code);
    }

    private sealed record HataYaniti(string Code, string Message);
}
