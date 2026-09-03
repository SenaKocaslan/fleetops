using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetOps.IntegrationTests;

// Iskelet dogrulamasi: gercek HTTP pipeline ayakta, moduller kendilerini
// IModule uzerinden kaydediyor ve uc noktalari eslesiyor. Fleet ve Tasks
// artik gercek uc noktalara sahip; onlar kendi test dosyalarinda.
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

    // Uc modulun de kendi uc noktasini esledigi. Gecici ping uc noktalari
    // gercek uc noktalarla degistirildi; burada yalnizca eslemenin
    // yapildigi kontrol ediliyor, davranis kendi test dosyalarinda.
    [Theory]
    [InlineData("/api/agvs")]
    [InlineData("/api/tasks")]
    [InlineData("/api/resources")]
    [InlineData("/api/locations")]
    [InlineData("/api/stock/movements")]
    public async Task Her_modul_kendi_uc_noktasini_esler(string yol)
    {
        var response = await _factory.CreateClient().GetAsync(yol);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
