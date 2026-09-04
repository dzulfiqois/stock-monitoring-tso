using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class UserAdminCreateTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public UserAdminCreateTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));

    [Fact]
    public async Task CreateUser_BySuperadmin_SucceedsWithRolesAndActiveRole()
    {
        using var scope = _factory.Services.CreateScope();
        var userAdmin = scope.ServiceProvider.GetRequiredService<IUserAdminService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        const string email = "newuser@stockmonitor.local";
        const string password = "Sandiku!2345";

        var userId = await userAdmin.CreateUserAsync(
            Principal("Superadmin"),
            email,
            password,
            new[] { "Operator", "Supervisi" },
            "Operator");

        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.ActiveRoleName.Should().Be("Operator");

        var roles = await userManager.GetRolesAsync(user);
        roles.Should().BeEquivalentTo(new[] { "Operator", "Supervisi" });

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        signInResult.Succeeded.Should().BeTrue("user baru harus dapat login dengan password awal");
    }

    [Fact]
    public async Task CreateUser_ByNonSuperadmin_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var userAdmin = scope.ServiceProvider.GetRequiredService<IUserAdminService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string email = "operator-created@stockmonitor.local";

        foreach (var role in new[] { "Operator", "Supervisi", "Tamu" })
        {
            var act = () => userAdmin.CreateUserAsync(
                Principal(role),
                email,
                "Sandiku!2345",
                new[] { role },
                role);
            await act.Should().ThrowAsync<UnauthorizedAccessException>($"{role} tidak boleh membuat user");
        }

        (await userManager.FindByEmailAsync(email)).Should().BeNull("tidak ada user tercipta");
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var userAdmin = scope.ServiceProvider.GetRequiredService<IUserAdminService>();

        await userAdmin.CreateUserAsync(
            Principal("Superadmin"),
            "dup@stockmonitor.local",
            "Sandiku!2345",
            new[] { "Tamu" },
            "Tamu");

        var act = () => userAdmin.CreateUserAsync(
            Principal("Superadmin"),
            "dup@stockmonitor.local",
            "SandikuLain!2345",
            new[] { "Operator" },
            "Operator");
        await act.Should().ThrowAsync<InvalidOperationException>("email sudah terdaftar");
    }

    [Fact]
    public async Task CreateUser_WeakPassword_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var userAdmin = scope.ServiceProvider.GetRequiredService<IUserAdminService>();

        var act = () => userAdmin.CreateUserAsync(
            Principal("Superadmin"),
            "weak@stockmonitor.local",
            "abc",
            new[] { "Tamu" },
            "Tamu");
        await act.Should().ThrowAsync<InvalidOperationException>("password harus sesuai policy Identity (min 8)");
    }

    [Fact]
    public async Task CreateUser_UnknownRole_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var userAdmin = scope.ServiceProvider.GetRequiredService<IUserAdminService>();

        var act = () => userAdmin.CreateUserAsync(
            Principal("Superadmin"),
            "norole@stockmonitor.local",
            "Sandiku!2345",
            new[] { "BukanRole" },
            "BukanRole");
        await act.Should().ThrowAsync<InvalidOperationException>("role tidak ada di master");
    }

    [Fact]
    public async Task CreateUser_ActiveRoleNotInRoles_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var userAdmin = scope.ServiceProvider.GetRequiredService<IUserAdminService>();

        var act = () => userAdmin.CreateUserAsync(
            Principal("Superadmin"),
            "badactive@stockmonitor.local",
            "Sandiku!2345",
            new[] { "Tamu" },
            "Operator");
        await act.Should().ThrowAsync<InvalidOperationException>("role aktif harus anggota roles");
    }

    [Fact]
    public async Task CreateUser_WritesAuditLog()
    {
        using var scope = _factory.Services.CreateScope();
        var userAdmin = scope.ServiceProvider.GetRequiredService<IUserAdminService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await userAdmin.CreateUserAsync(
            Principal("Superadmin"),
            "audit@stockmonitor.local",
            "Sandiku!2345",
            new[] { "Operator", "Tamu" },
            "Tamu");

        var entry = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == "CreateUser" && a.EntityType == "ApplicationUser")
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        entry.Should().NotBeNull("audit CreateUser harus tercatat");
        entry!.After.Should().Be("Operator,Tamu");
        entry.ActorRole.Should().Be("Superadmin");
        entry.Detail.Should().Contain("audit@stockmonitor.local");
    }
}