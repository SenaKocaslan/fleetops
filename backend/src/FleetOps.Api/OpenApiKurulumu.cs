using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace FleetOps.Api;

public static class OpenApiKurulumu
{
    private const string BearerSemasi = "Bearer";

    public static IServiceCollection AddFleetOpsOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(secenekler =>
        {
            secenekler.AddDocumentTransformer((belge, _, _) =>
            {
                belge.Info.Title = "FleetOps API";
                belge.Components ??= new OpenApiComponents();
                belge.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                belge.Components.SecuritySchemes[BearerSemasi] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "POST /api/auth/login ile alinan token.",
                };

                return Task.CompletedTask;
            });

            // Bu donusturucu olmadan dokuman hangi uc noktanin token istedigini
            // soylemez; yetki bilgisi yalnizca uc nokta meta verisinde durur.
            // Politika adlari da yaziliyor: "token lazim" yetmez, operator ile
            // supervisor ayni uc noktayi cagiramayabilir.
            secenekler.AddOperationTransformer(async (islem, baglam, _) =>
            {
                var meta = baglam.Description.ActionDescriptor.EndpointMetadata;
                var yetkiler = meta.OfType<IAuthorizeData>().ToList();

                if (yetkiler.Count == 0 || meta.OfType<IAllowAnonymous>().Any())
                {
                    return;
                }

                islem.Security ??= [];
                islem.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerSemasi, baglam.Document)] = [],
                });

                // Roller elle yazilmiyor, politikanin GERCEK tanimindan okunuyor:
                // AuthKurulumu'nda bir rol degisirse dokuman da degisir.
                var saglayici = baglam.ApplicationServices
                    .GetRequiredService<IAuthorizationPolicyProvider>();

                // Grup ve uc nokta ayri politika koyabiliyor; ikisi de gecmeli.
                var parcalar = new List<string>();
                foreach (var ad in yetkiler.Select(y => y.Policy).OfType<string>().Distinct())
                {
                    var politika = await saglayici.GetPolicyAsync(ad);
                    var roller = politika?.Requirements
                        .OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>()
                        .SelectMany(r => r.AllowedRoles)
                        .ToList() ?? [];

                    parcalar.Add(roller.Count == 0
                        ? $"{ad} (giris yapmis herkes)"
                        : $"{ad} ({string.Join(", ", roller)})");
                }

                islem.Description = "Gereken politika: " + string.Join(" + ", parcalar);
            });
        });
}
