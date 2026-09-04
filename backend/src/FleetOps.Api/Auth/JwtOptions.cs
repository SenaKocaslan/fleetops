namespace FleetOps.Api.Auth;

public sealed class JwtOptions
{
    public const string Bolum = "Jwt";

    public string Issuer { get; set; } = "fleetops";

    public string Audience { get; set; } = "fleetops-web";

    // Uretimde ortam degiskeninden gelir; appsettings'e gercek anahtar yazilmaz.
    public string SigningKey { get; set; } = string.Empty;

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(8);
}
