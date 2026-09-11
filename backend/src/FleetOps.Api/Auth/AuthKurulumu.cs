using FleetOps.SharedKernel;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FleetOps.Api.Auth;

public static class AuthKurulumu
{
    public static IServiceCollection AddFleetOpsAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("FleetOps"),
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    AuthDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Bolum));
        services.AddScoped<TokenUretici>();

        services.AddMemoryCache();
        services.Configure<GirisOptions>(configuration.GetSection(GirisOptions.Bolum));
        services.AddSingleton<GirisDenemeSayaci>();

        var ayarlar = configuration.GetSection(JwtOptions.Bolum).Get<JwtOptions>() ?? new JwtOptions();

        // Anahtar bos gelirse HMAC-SHA256 yine de calisir ve uygulama sessizce
        // kalkar; o an uretilen her token sahte token'dan ayirt edilemez.
        // Bu yuzden acilista patlamasi dogru davranis. 32 bayt HMAC-SHA256'nin
        // asgarisi -- kisasi zaten IDX10653 ile calisma aninda patlardi.
        if (Encoding.UTF8.GetByteCount(ayarlar.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey en az 32 bayt olmali. Uretimde ortam degiskeninden " +
                "verilir: Jwt__SigningKey");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(secenekler =>
            {
                secenekler.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = ayarlar.Issuer,
                    ValidAudience = ayarlar.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(ayarlar.SigningKey)),

                    // Varsayilan 5 dakikalik tolerans, "token suresi doldu"
                    // testini 5 dakika beklemeye zorlar.
                    ClockSkew = TimeSpan.Zero,
                };

                // Tarayici WebSocket el sikismasinda Authorization basligi
                // gonderemez; SignalR token'i sorgu dizesinde tasir.
                secenekler.Events = new JwtBearerEvents
                {
                    OnMessageReceived = baglam =>
                    {
                        var token = baglam.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(token) &&
                            baglam.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            baglam.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politikalar.Okuma, p => p.RequireAuthenticatedUser())
            .AddPolicy(Politikalar.GorevYurutme, p =>
                p.RequireRole(Roller.Operator, Roller.Supervisor))
            .AddPolicy(Politikalar.GorevPlanlama, p => p.RequireRole(Roller.Supervisor))
            .AddPolicy(Politikalar.Telemetri, p =>
                p.RequireRole(Roller.Operator, Roller.Supervisor));

        return services;
    }

    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grup = endpoints.MapGroup("/api/auth").WithTags("Auth");

        grup.MapPost("/login", async (
            LoginIstegi istek,
            AuthDbContext db,
            TokenUretici uretici,
            GirisDenemeSayaci sayac,
            CancellationToken ct) =>
        {
            // Hash hesaplanmadan ONCE: kilitli hesaba yapilan deneme sunucuya
            // PBKDF2 maliyeti odetmesin.
            if (sayac.KilitliMi(istek.UserName))
            {
                return Results.StatusCode(StatusCodes.Status429TooManyRequests);
            }

            var kullanici = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.UserName == istek.UserName, ct);

            // Kullanici yok ile parola yanlis AYNI yaniti ve AYNI SUREYI verir.
            // Dogrulama her durumda calisir; kullanici yoksa ayni maliyetteki
            // karsilastirma hash'ine karsi.
            var parolaDogru = ParolaHashleyici.Dogrula(
                istek.Password,
                kullanici?.PasswordHash ?? ParolaHashleyici.KarsilastirmaHashi);

            if (kullanici is null || !parolaDogru)
            {
                sayac.BasarisizKaydet(istek.UserName);
                return Results.Unauthorized();
            }

            sayac.Sifirla(istek.UserName);

            var (token, bitis) = uretici.Uret(kullanici);

            return Results.Ok(new LoginYaniti(token, bitis, kullanici.UserName, kullanici.Role));
        });

        grup.MapGet("/me", (ClaimsPrincipal kullanici) => Results.Ok(new
        {
            userName = kullanici.Identity?.Name,
            role = kullanici.FindFirstValue(ClaimTypes.Role),
        })).RequireAuthorization(Politikalar.Okuma);
    }
}

public sealed record LoginIstegi(string UserName, string Password);

public sealed record LoginYaniti(string Token, DateTime ExpiresAtUtc, string UserName, string Role);
