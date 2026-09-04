using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FleetOps.Api.Auth;

internal sealed class TokenUretici(IOptions<JwtOptions> ayarlar)
{
    private readonly JwtOptions _ayarlar = ayarlar.Value;

    public (string Token, DateTime ExpiresAtUtc) Uret(AppUser kullanici)
    {
        var bitis = DateTime.UtcNow.Add(_ayarlar.Lifetime);

        var iddialar = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, kullanici.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, kullanici.UserName),
            new Claim(ClaimTypes.Role, kullanici.Role),
        };

        var anahtar = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ayarlar.SigningKey));

        var token = new JwtSecurityToken(
            issuer: _ayarlar.Issuer,
            audience: _ayarlar.Audience,
            claims: iddialar,
            expires: bitis,
            signingCredentials: new SigningCredentials(anahtar, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), bitis);
    }
}
