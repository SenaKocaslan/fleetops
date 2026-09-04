using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FleetOps.Api.Auth;
using FleetOps.IntegrationTests.Altyapi;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public class YetkilendirmeTests(FleetOpsApiFactory fabrika)
{
    private static readonly Guid Agv01 = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Dogru_bilgiyle_giris_token_doner()
    {
        var yanit = await fabrika.CreateClient().PostAsJsonAsync(
            "/api/auth/login",
            new { userName = "supervisor", password = "Supervisor123!" });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var govde = await yanit.Content.ReadFromJsonAsync<LoginYaniti>();
        Assert.False(string.IsNullOrWhiteSpace(govde!.Token));
        Assert.Equal("Supervisor", govde.Role);
        Assert.True(govde.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Theory]
    [InlineData("supervisor", "yanlis-parola")]
    [InlineData("olmayan-kullanici", "Supervisor123!")]
    public async Task Yanlis_bilgi_401_doner(string kullanici, string parola)
    {
        var yanit = await fabrika.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { userName = kullanici, password = parola });

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Theory]
    [InlineData("/api/agvs")]
    [InlineData("/api/tasks")]
    [InlineData("/api/resources")]
    [InlineData("/api/locations")]
    [InlineData("/api/stock/movements")]
    public async Task Tokensiz_okuma_401_doner(string yol)
    {
        var yanit = await fabrika.CreateClient().GetAsync(yol);

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact]
    public async Task Bozuk_token_401_doner()
    {
        var istemci = fabrika.CreateClient();
        istemci.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "bu.bir.token-degil");

        var yanit = await istemci.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact]
    public async Task Baska_anahtarla_imzalanmis_token_401_doner()
    {
        // Payload dogru gorunse bile imza tutmuyorsa kabul edilmemeli.
        var gecerli = await fabrika.TokenAsync();
        var bozulmus = gecerli[..^4] + (gecerli[^4..] == "AAAA" ? "BBBB" : "AAAA");

        // Once saglam token'in GECTIGI dogrulaniyor. Bu satir olmazsa test,
        // "her sey 401 donuyor" durumunda da yesil yanar; yani imza
        // dogrulamasi kapatildiginda kirmizi yanmaz.
        var saglamIstemci = fabrika.CreateClient();
        saglamIstemci.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", gecerli);
        Assert.Equal(HttpStatusCode.OK, (await saglamIstemci.GetAsync("/api/tasks")).StatusCode);

        var istemci = fabrika.CreateClient();
        istemci.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bozulmus);

        var yanit = await istemci.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact]
    public async Task Operator_gorev_olusturamaz()
    {
        var istemci = await fabrika.IstemciAsync(
            FleetOpsApiFactory.OperatorAdi, FleetOpsApiFactory.OperatorParolasi);

        var yanit = await istemci.PostAsJsonAsync("/api/tasks", YeniGorev());

        // 401 degil 403: kim oldugu belli, yetkisi yok.
        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    [Fact]
    public async Task Supervisor_gorev_olusturabilir()
    {
        var istemci = await fabrika.IstemciAsync();

        var yanit = await istemci.PostAsJsonAsync("/api/tasks", YeniGorev());

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
    }

    [Fact]
    public async Task Operator_gorevi_baslatabilir()
    {
        var supervisor = await fabrika.IstemciAsync();
        var olusturma = await supervisor.PostAsJsonAsync("/api/tasks", YeniGorev());
        var gorevId = (await olusturma.Content.ReadFromJsonAsync<OlusturmaYaniti>())!.Id;
        await supervisor.PostAsJsonAsync($"/api/tasks/{gorevId}/assign", new { agvId = Agv01 });

        var operatorIstemci = await fabrika.IstemciAsync(
            FleetOpsApiFactory.OperatorAdi, FleetOpsApiFactory.OperatorParolasi);

        var yanit = await operatorIstemci.PostAsync($"/api/tasks/{gorevId}/start", null);

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);

        // AGV'yi serbest birak, sonraki testler Busy bir filoyla karsilasmasin.
        await operatorIstemci.PostAsync($"/api/tasks/{gorevId}/complete", null);
    }

    [Fact]
    public async Task Operator_telemetri_gonderebilir()
    {
        var istemci = await fabrika.IstemciAsync(
            FleetOpsApiFactory.OperatorAdi, FleetOpsApiFactory.OperatorParolasi);

        var yanit = await istemci.PostAsJsonAsync(
            $"/api/agvs/{Agv01}/telemetry", new { batteryLevel = 88 });

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);
    }

    [Fact]
    public async Task Me_uc_noktasi_token_icindeki_rolu_doner()
    {
        var istemci = await fabrika.IstemciAsync(
            FleetOpsApiFactory.OperatorAdi, FleetOpsApiFactory.OperatorParolasi);

        var govde = await istemci.GetFromJsonAsync<MeYaniti>("/api/auth/me");

        Assert.Equal("operator", govde!.UserName);
        Assert.Equal("Operator", govde.Role);
    }

    private static object YeniGorev() => new
    {
        fromLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
        toLocationId = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
        materialCode = $"YTK-{Guid.NewGuid():N}"[..12],
        quantity = 1,
        priority = 5,
    };

    private sealed record OlusturmaYaniti(Guid Id);

    private sealed record MeYaniti(string UserName, string Role);
}
