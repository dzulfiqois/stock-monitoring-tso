using System.Security.Claims;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

public interface IUserAdminService
{
    Task<IReadOnlyList<ApplicationUser>> ListUsersAsync(CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct = default);

    Task AssignRoleAsync(ClaimsPrincipal actor, string userId, string roleName, CancellationToken ct = default);

    Task RemoveRoleAsync(ClaimsPrincipal actor, string userId, string roleName, CancellationToken ct = default);

    Task SetPasswordAsync(ClaimsPrincipal actor, string userId, string newPassword, CancellationToken ct = default);

    Task<string> CreateUserAsync(
        ClaimsPrincipal actor,
        string email,
        string password,
        IReadOnlyList<string> roles,
        string activeRole,
        CancellationToken ct = default);
}
