using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetOps.IntegrationTests;

public class ModuleRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ModuleRegistrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

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
