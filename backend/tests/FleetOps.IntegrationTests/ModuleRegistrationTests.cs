using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetOps.IntegrationTests;

/// <summary>
/// Iskelet dogrulamasi: gercek HTTP pipeline ayakta, moduller kendilerini
/// IModule uzerinden kaydediyor ve uc noktalari eslesiyor.
/// </summary>
public class ModuleRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ModuleRegistrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Saglik_uc_noktasi_cevap_verir()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/fleet/ping", "Fleet")]
    [InlineData("/api/tasks/ping", "Tasks")]
    [InlineData("/api/stock/ping", "Stock")]
    public async Task Her_modul_kendi_uc_noktasini_esler(string yol, string beklenenModul)
    {
        var response = await _factory.CreateClient().GetAsync(yol);
        response.EnsureSuccessStatusCode();

        var govde = await response.Content.ReadFromJsonAsync<PingYaniti>();

        Assert.Equal(beklenenModul, govde?.Module);
    }

    private sealed record PingYaniti(string Module);
}
