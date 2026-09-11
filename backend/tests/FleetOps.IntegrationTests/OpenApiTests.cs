using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FleetOps.IntegrationTests.Altyapi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FleetOps.IntegrationTests;

[Collection(VeritabaniKoleksiyonu.Ad)]
public partial class OpenApiTests(FleetOpsApiFactory fabrika)
{
    [Fact]
    public async Task Her_api_uc_noktasi_dokumanda_yer_alir()
    {
        // Dokuman eksik kalirsa kimse fark etmez: okuyan kisi olmayan uc
        // noktayi aramaz. Gercek uc nokta listesiyle karsilastiriliyor.
        var dokuman = await DokumanAsync();
        var belgedekiler = dokuman.GetProperty("paths").EnumerateObject()
            .SelectMany(y => y.Value.EnumerateObject().Select(i => $"{i.Name.ToUpperInvariant()} {y.Name}"))
            .ToHashSet();

        var gercekler = fabrika.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(u => u.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
            .SelectMany(u => (u.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                .Select(y => $"{y} {Normallestir(u.RoutePattern.RawText!)}"))
            .ToHashSet();

        Assert.NotEmpty(gercekler);
        Assert.Empty(gercekler.Except(belgedekiler));
    }

    [Fact]
    public async Task Giris_anonim_digerleri_token_ve_rol_bilgisi_tasir()
    {
        var yollar = (await DokumanAsync()).GetProperty("paths");

        var giris = yollar.GetProperty("/api/auth/login").GetProperty("post");
        Assert.False(giris.TryGetProperty("security", out _));

        var gorevAc = yollar.GetProperty("/api/tasks").GetProperty("post");
        Assert.True(gorevAc.TryGetProperty("security", out _));

        // Rol, politikanin gercek tanimindan okunuyor; elle yazilmiyor.
        var aciklama = gorevAc.GetProperty("description").GetString();
        Assert.Contains("Supervisor", aciklama);
        Assert.DoesNotContain("Operator", aciklama);
    }

    [Fact]
    public async Task Uretim_ortaminda_dokuman_yayinlanmaz()
    {
        await using var uretim = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("ConnectionStrings:FleetOps", fabrika.BaglantiMetni);
            b.UseSetting("Jwt:SigningKey", "test-imza-anahtari-en-az-32-bayt-uzunlugunda-olmali");
            b.UseSetting("Outbox:PollInterval", "01:00:00");
            b.UseSetting("Outbox:CleanupInterval", "01:00:00");
            b.UseSetting("ResourceLock:ReaperInterval", "01:00:00");
            b.UseSetting("Sarj:Interval", "01:00:00");
        });

        var yanit = await uretim.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
    }

    private async Task<JsonElement> DokumanAsync()
    {
        var yanit = await fabrika.CreateClient().GetAsync("/openapi/v1.json");
        yanit.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;
    }

    // "/api/tasks/{id:guid}/assign" -> "/api/tasks/{id}/assign": OpenAPI yol
    // kisitlarini tasimiyor.
    private static string Normallestir(string desen) =>
        KisitDeseni().Replace(desen, "{$1}").TrimEnd('/') is { Length: > 0 } y ? y : "/";

    [GeneratedRegex(@"\{(\w+):[^}]+\}")]
    private static partial Regex KisitDeseni();
}
