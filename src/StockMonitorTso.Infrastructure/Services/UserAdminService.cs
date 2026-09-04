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

    public async Task<string> CreateUserAsync(
        ClaimsPrincipal actor,
        string email,
        string password,
        IReadOnlyList<string> roles,
        string activeRole,
        CancellationToken ct = default)
    {
        RequireSuperadmin(actor);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email wajib diisi.");
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Password wajib diisi.");
        }
        if (roles is null || roles.Count == 0)
        {
            throw new InvalidOperationException("Minimal satu role harus dipilih.");
        }
        if (string.IsNullOrWhiteSpace(activeRole) || !roles.Contains(activeRole))
        {
            throw new InvalidOperationException("Role aktif harus salah satu role yang dipilih.");
        }

        var normalizedEmail = email.Trim();
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            throw new InvalidOperationException($"Email '{normalizedEmail}' sudah terdaftar.");
        }

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                throw new InvalidOperationException($"Role '{role}' tidak terdaftar.");
            }
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            ActiveRoleName = activeRole,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Gagal membuat user: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        var addRolesResult = await userManager.AddToRolesAsync(user, roles);
        if (!addRolesResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"User dibuat, namun gagal menetapkan role: {string.Join("; ", addRolesResult.Errors.Select(e => e.Description))}");
        }

        await auditLog.LogAsync(new AuditLog
        {
            ActorUserId = userManager.GetUserId(actor),
            ActorEmail = userManager.GetUserName(actor),
            ActorRole = GetActiveRole(actor),
            Action = "CreateUser",
            EntityType = "ApplicationUser",
            EntityId = user.Id,
            After = string.Join(",", roles),
            Detail = $"Email={normalizedEmail}; ActiveRole={activeRole}",
        }, ct);

        return user.Id;
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
