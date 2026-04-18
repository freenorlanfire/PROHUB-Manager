using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Services;

public record JwtSettings
{
    public string Key { get; init; } = "";
    public string Issuer { get; init; } = "prohub-api";
    public string Audience { get; init; } = "prohub-app";
    public int ExpiresMinutes { get; init; } = 60;
}

public class AuthService(JwtSettings jwt)
{
    // ── JWT ───────────────────────────────────────────────────────────────────────

    public (string token, DateTime expiresAt) GenerateToken(User user, Guid? companyId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.Name),
            new("role", user.Role),
        };

        if (companyId.HasValue && companyId != Guid.Empty)
            claims.Add(new("companyId", companyId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // ── Refresh tokens ────────────────────────────────────────────────────────────

    /// <summary>Generates a random 64-byte token (URL-safe base64).</summary>
    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
               .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>SHA-256 of the raw token — what is stored in DB.</summary>
    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    // ── Passwords (PBKDF2) ────────────────────────────────────────────────────────

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expected = Convert.FromBase64String(parts[1]);
        }
        catch { return false; }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
