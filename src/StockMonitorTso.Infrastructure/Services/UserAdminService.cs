using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Abstractions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

public sealed class UserAdminService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAuditLogService auditLog) : IUserAdminService
{
    public const string Superadmin = "Superadmin";

    public async Task<IReadOnlyList<ApplicationUser>> ListUsersAsync(CancellationToken ct = default)
    {
        return await db.Users.OrderBy(u => u.Email).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User tidak ditemukan.");
        return (await userManager.GetRolesAsync(user)).ToList();
    }

    public async Task AssignRoleAsync(ClaimsPrincipal actor, string userId, string roleName, CancellationToken ct = default)
    {
        RequireSuperadmin(actor);

        var user = await userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User tidak ditemukan.");
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException($"Role '{roleName}' tidak terdaftar.");
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
            await auditLog.LogAsync(new AuditLog
            {
                ActorUserId = userManager.GetUserId(actor),
                ActorEmail = userManager.GetUserName(actor),
                ActorRole = GetActiveRole(actor),
                Action = "AssignRole",
                EntityType = "ApplicationUser",
                EntityId = userId,
                After = roleName,
            }, ct);
        }

        if (user.ActiveRoleName is null)
        {
            user.ActiveRoleName = roleName;
            await userManager.UpdateAsync(user);
        }
    }

    public async Task RemoveRoleAsync(ClaimsPrincipal actor, string userId, string roleName, CancellationToken ct = default)
    {
        RequireSuperadmin(actor);

        var user = await userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User tidak ditemukan.");
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            return;
        }

        await userManager.RemoveFromRoleAsync(user, roleName);
        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = userManager.GetUserId(actor),
            ActorEmail = userManager.GetUserName(actor),
            ActorRole = GetActiveRole(actor),
            Action = "RemoveRole",
            EntityType = "ApplicationUser",
            EntityId = userId,
            Before = roleName,
        }, ct);

        if (user.ActiveRoleName == roleName)
        {
            var remaining = await userManager.GetRolesAsync(user);
            user.ActiveRoleName = remaining.FirstOrDefault();
            await userManager.UpdateAsync(user);
        }
    }

    public async Task SetPasswordAsync(ClaimsPrincipal actor, string userId, string newPassword, CancellationToken ct = default)
    {
        RequireSuperadmin(actor);

        var user = await userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User tidak ditemukan.");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Gagal mengganti password: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = userManager.GetUserId(actor),
            ActorEmail = userManager.GetUserName(actor),
            ActorRole = GetActiveRole(actor),
            Action = "ChangePassword",
            EntityType = "ApplicationUser",
            EntityId = userId,
            Detail = "Password diubah oleh Superadmin.",
        }, ct);
    }

    private static void RequireSuperadmin(ClaimsPrincipal actor)
    {
        if (!actor.IsInRole(Superadmin))
        {
            throw new UnauthorizedAccessException("Hanya Superadmin yang dapat melakukan aksi ini.");
        }
    }

    private static string? GetActiveRole(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.Role);
}
