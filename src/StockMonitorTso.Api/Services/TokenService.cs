using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Api.Services;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) IssueAccessToken(ApplicationUser user, IList<string> roles, string activeRole);
    string IssueRefreshToken(string userId);
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    private const int AccessMinutes = 15;
    private const int RefreshDays = 7;

    private SymmetricSecurityKey SigningKey => new(
        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key belum dikonfigurasi (env: Jwt__Key).")));

    private string Issuer => configuration["Jwt:Issuer"] ?? "stockmonitor-api";
    private string Audience => configuration["Jwt:Audience"] ?? "stockmonitor-client";

    public (string AccessToken, DateTime ExpiresAt) IssueAccessToken(ApplicationUser user, IList<string> roles, string activeRole)
    {
        var expires = DateTime.UtcNow.AddMinutes(AccessMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, activeRole),
            new("active_role", activeRole),
        };
        foreach (var role in roles)
        {
            claims.Add(new Claim("member_roles", role));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string IssueRefreshToken(string userId)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: $"{Audience}/refresh",
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new("typ", "refresh"),
            ],
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(RefreshDays),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = $"{Audience}/refresh",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        try
        {
            var principal = handler.ValidateToken(refreshToken, parameters, out var raw);
            if (raw is not JwtSecurityToken jwt
                || !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)
                || principal.FindFirst("typ")?.Value != "refresh")
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }
}
