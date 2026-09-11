using System.Net;
using FleetOps.IntegrationTests.Altyapi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetOps.IntegrationTests;

// Onceki saglik testi duz WebApplicationFactory ile calisiyordu; uc nokta
// veritabanina bakmadigi icin geciyordu. Artik bakiyor: o test ya
// gelistirme veritabanina (55432) baglanir ya da CI'da 503 alirdi. Bu yuzden
// "saglikli" durumu gercek veritabaniyla, "sagliksiz" durumu ulasilamayan
// bir adresle sinaniyor.
[Collection(VeritabaniKoleksiyonu.Ad)]
public class SaglikTests(FleetOpsApiFactory fabrika)
{
    [Fact]
    public async Task Veritabani_ayaktayken_saglikli()
    {
        var yanit = await fabrika.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal("Healthy", await yanit.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Veritabanina_ulasilamazsa_sagliksiz()
    {
        await using var fabrikaDbsiz = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            // Port 1: yerelde dinleyen hicbir sey yok, baglanti aninda reddedilir.
            b.UseSetting("ConnectionStrings:FleetOps",
                "Host=127.0.0.1;Port=1;Database=yok;Username=yok;Password=yok;Timeout=2");
            b.UseSetting("Simulator:Enabled", "false");
            b.UseSetting("Outbox:PollInterval", "01:00:00");
            b.UseSetting("ResourceLock:ReaperInterval", "01:00:00");
            b.UseSetting("Sarj:Interval", "01:00:00");
        });

        var yanit = await fabrikaDbsiz.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, yanit.StatusCode);
        Assert.Equal("Unhealthy", await yanit.Content.ReadAsStringAsync());
    }
}
