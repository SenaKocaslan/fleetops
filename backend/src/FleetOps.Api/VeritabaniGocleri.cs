using FleetOps.Api.Auth;
using FleetOps.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Api;

// Goc ACILISTA calismaz. Iki kopya ayni anda kalkarsa ikisi de goc uygulamaya
// calisir; ayrica uygulamanin veritabani semasini degistirme yetkisi olmasi
// gerekir. Bu yuzden ayri bir adim: "dotnet FleetOps.Api.dll --migrate"
// (compose'da ayri bir servis, api ondan sonra baslar).
public static class VeritabaniGocleri
{
    public const string Arguman = "--migrate";

    public static async Task UygulaAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var kapsam = services.CreateScope();

        // Auth semasi bir modul degil, composition root'a ait.
        await kapsam.ServiceProvider.GetRequiredService<AuthDbContext>()
            .Database.MigrateAsync(cancellationToken);

        foreach (var modul in kapsam.ServiceProvider.GetServices<IModule>())
        {
            await modul.MigrateAsync(kapsam.ServiceProvider, cancellationToken);
        }
    }
}
