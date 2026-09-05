using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Api.Services;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class AuthAndRbacTests : IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactory _factory;

    public AuthAndRbacTests(TestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seed_CreatesRolesAndUsers()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Superadmin", "Operator", "Supervisi", "Tamu" })
        {
            (await roleManager.RoleExistsAsync(role)).Should().BeTrue($"role {role} harus ada");
        }

        var superadmin = await userManager.FindByEmailAsync("superadmin@stockmonitor.local");
        superadmin.Should().NotBeNull();
        var roles = await userManager.GetRolesAsync(superadmin!);
        roles.Should().Contain("Superadmin");
    }

    [Fact]
    public async Task ClaimsFactory_OnlyActiveRoleIsClaim()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var factory = sp.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("multi@stockmonitor.local");
        user.Should().NotBeNull();

        user!.ActiveRoleName = "Supervisi";
        await userManager.UpdateAsync(user);

        var principal = await factory.CreateAsync(user);
        principal.IsInRole("Supervisi").Should().BeTrue();
        principal.IsInRole("Operator").Should().BeFalse("hanya role aktif yang menjadi klaim");
        principal.IsInRole("Tamu").Should().BeFalse();
    }

    [Fact]
    public async Task UserAdminService_AssignRole_SuperadminAllowed()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userAdmin = sp.GetRequiredService<IUserAdminService>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("tamu@stockmonitor.local");
        user.Should().NotBeNull();

        var superadmin = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Superadmin") }, "Test"));

        await userAdmin.AssignRoleAsync(superadmin, user!.Id, "Operator");

        var roles = await userManager.GetRolesAsync(user);
        roles.Should().Contain("Operator");
    }

    [Fact]
    public async Task UserAdminService_AssignRole_NonSuperadminRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userAdmin = sp.GetRequiredService<IUserAdminService>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("tamu@stockmonitor.local");
        user.Should().NotBeNull();

        var operatorPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Operator") }, "Test"));

        var act = () => userAdmin.AssignRoleAsync(operatorPrincipal, user!.Id, "Supervisi");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UserAdminService_SetPassword_NonSuperadminRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userAdmin = sp.GetRequiredService<IUserAdminService>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("tamu@stockmonitor.local");
        user.Should().NotBeNull();

        var operatorPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Operator") }, "Test"));

        var act = () => userAdmin.SetPasswordAsync(operatorPrincipal, user!.Id, "NewPass!123");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AccessToken_ExpiryIs15Minutes()
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("superadmin@stockmonitor.local");
        user.Should().NotBeNull();

        var (accessToken, _) = tokenService.IssueAccessToken(user, new List<string> { "Superadmin" }, "Superadmin");
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        var lifetime = jwt.ValidTo - jwt.ValidFrom;
        lifetime.Should().Be(TimeSpan.FromMinutes(15));
        jwt.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Superadmin");
    }
}
