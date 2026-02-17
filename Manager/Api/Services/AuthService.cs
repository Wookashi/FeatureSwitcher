using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class AuthService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IUserRepository _userRepository;

    public AuthService(IOptions<JwtSettings> jwtSettings, IUserRepository userRepository)
    {
        _jwtSettings = jwtSettings.Value;
        _userRepository = userRepository;
    }

    public bool ValidateCredentials(string username, string password)
    {
        var hash = _userRepository.GetPasswordHash(username);
        if (hash is null) return false;
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public (string Token, DateTime ExpiresAt, string Role) GenerateToken(string username)
    {
        var user = _userRepository.GetUserByUsername(username);
        if (user is null)
            throw new InvalidOperationException($"User '{username}' not found.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt, user.Role);
    }
}
