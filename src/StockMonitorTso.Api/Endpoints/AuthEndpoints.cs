using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Api.Services;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Api.Endpoints;

public static class AuthEndpoints
{
    public sealed record LoginRequest(string Email, string Password, string? ActiveRole);

    public sealed record SwitchRoleRequest(string Role);

    public sealed record AuthResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresInMinutes,
        string Email,
        string ActiveRole,
        IList<string> Roles);

    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            ITokenService tokens) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return Results.Problem("Email atau password salah.", statusCode: 401);
            }

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                return Results.Problem("User belum memiliki role.", statusCode: 403);
            }

            var activeRole = ResolveActiveRole(user, request.ActiveRole, roles);
            if (activeRole is null)
            {
                return Results.Problem($"Role '{request.ActiveRole}' bukan role user ini.", statusCode: 400);
            }

            if (user.ActiveRoleName != activeRole)
            {
                user.ActiveRoleName = activeRole;
                await userManager.UpdateAsync(user);
            }

            return Results.Ok(BuildResponse(tokens, user, roles, activeRole));
        });

        group.MapPost("/refresh", async (
            RefreshRequest request,
            UserManager<ApplicationUser> userManager,
            ITokenService tokens) =>
        {
            var principal = tokens.ValidateRefreshToken(request.RefreshToken);
            if (principal is null)
            {
                return Results.Problem("Refresh token tidak valid atau kedaluwarsa.", statusCode: 401);
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var user = await userManager.FindByIdAsync(userId ?? "");
            if (user is null)
            {
                return Results.Problem("User tidak ditemukan.", statusCode: 401);
            }

            var roles = await userManager.GetRolesAsync(user);
            var activeRole = ResolveActiveRole(user, null, roles)
                ?? throw new InvalidOperationException("User tidak memiliki role aktif yang valid.");

            return Results.Ok(BuildResponse(tokens, user, roles, activeRole));
        });

        group.MapPost("/logout", async (HttpContext http) =>
        {
            await Task.CompletedTask;
            return Results.NoContent();
        }).AllowAnonymous();

        group.MapGet("/me", async (
            HttpContext http,
            UserManager<ApplicationUser> userManager) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId ?? "");
            if (user is null)
            {
                return Results.Problem("User tidak ditemukan.", statusCode: 401);
            }

            var roles = await userManager.GetRolesAsync(user);
            var activeRole = http.User.FindFirstValue(ClaimTypes.Role) ?? user.ActiveRoleName ?? roles.FirstOrDefault() ?? "";
            return Results.Ok(new MeResponse(user.Email ?? "", activeRole, roles));
        }).RequireAuthorization();

        group.MapPost("/switch-role", async (
            SwitchRoleRequest request,
            HttpContext http,
            UserManager<ApplicationUser> userManager,
            ITokenService tokens) =>
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId ?? "");
            if (user is null)
            {
                return Results.Problem("User tidak ditemukan.", statusCode: 401);
            }

            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            {
                return Results.Problem($"Role '{request.Role}' bukan role user ini.", statusCode: 400);
            }

            user.ActiveRoleName = roles.First(r => string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));
            await userManager.UpdateAsync(user);

            return Results.Ok(BuildResponse(tokens, user, roles, user.ActiveRoleName));
        }).RequireAuthorization();

        return group;
    }

    private static string? ResolveActiveRole(ApplicationUser user, string? requested, IList<string> roles)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return roles.FirstOrDefault(r => string.Equals(r, requested, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(user.ActiveRoleName)
            && roles.Any(r => string.Equals(r, user.ActiveRoleName, StringComparison.OrdinalIgnoreCase)))
        {
            return user.ActiveRoleName;
        }

        return roles.FirstOrDefault();
    }

    private static AuthResponse BuildResponse(ITokenService tokens, ApplicationUser user, IList<string> roles, string activeRole)
    {
        var (accessToken, expiresAt) = tokens.IssueAccessToken(user, roles, activeRole);
        var refresh = tokens.IssueRefreshToken(user.Id);
        var minutes = (int)Math.Round((expiresAt - DateTime.UtcNow).TotalMinutes);
        return new AuthResponse(accessToken, refresh, minutes, user.Email ?? "", activeRole, roles);
    }

    public sealed record RefreshRequest(string RefreshToken);

    public sealed record MeResponse(string Email, string ActiveRole, IList<string> Roles);
}
