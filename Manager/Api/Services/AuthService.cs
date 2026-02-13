using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wookashi.FeatureSwitcher.Manager.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class AuthService(IOptions<JwtSettings> jwtSettings, IOptions<AdminCredentials> adminCredentials)
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly AdminCredentials _adminCredentials = adminCredentials.Value;

    public bool ValidateCredentials(string username, string password)
    {
        return string.Equals(username, _adminCredentials.Username, StringComparison.OrdinalIgnoreCase)
               && string.Equals(password, _adminCredentials.Password, StringComparison.Ordinal);
    }

    public (string Token, DateTime ExpiresAt) GenerateToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, _adminCredentials.Username),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
