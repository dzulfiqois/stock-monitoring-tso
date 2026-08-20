using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StockMonitorTso.Infrastructure.Persistence;

namespace StockMonitorTso.Infrastructure.Services;

/// <summary>
/// Menghasilkan klaim role hanya untuk role aktif user (bukan seluruh role).
/// Sesuai STOCK_MONITORING_SPEC.md §6.2: hak akses mengikuti role aktif, bukan gabungan.
/// </summary>
public sealed class ActiveRoleClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ActiveRoleClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var roleClaims = identity.FindAll(identity.RoleClaimType).ToList();
        foreach (var claim in roleClaims)
        {
            identity.RemoveClaim(claim);
        }

        if (!string.IsNullOrWhiteSpace(user.ActiveRoleName))
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, user.ActiveRoleName));
        }

        return identity;
    }
}
