using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockMonitorTso.IntegrationTests;

public class UsersApiTests : IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactory _factory;

    public UsersApiTests(TestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_BySuperadmin_ReturnsSeededUsers()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");

        var response = await superadmin.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users!.Select(u => u.Email).Should().Contain("superadmin@stockmonitor.local", "multi@stockmonitor.local");
    }

    [Fact]
    public async Task List_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        (await client.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_ByTamu_Rejected403()
    {
        var tamu = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        (await tamu.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_ThenLoginWithNewPassword()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");

        var create = await superadmin.PostAsJsonAsync("/api/users", new
        {
            email = "r1user@stockmonitor.local",
            password = "R1User!2345",
            roles = new[] { "Operator", "Tamu" },
            activeRole = "Operator",
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            email = "r1user@stockmonitor.local",
            password = "R1User!2345",
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth!.ActiveRole.Should().Be("Operator");
        auth.Roles.Should().BeEquivalentTo("Operator", "Tamu");
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Rejected400()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");

        var request = new
        {
            email = "r1dup@stockmonitor.local",
            password = "R1Dup!2345",
            roles = new[] { "Tamu" },
            activeRole = "Tamu",
        };
        (await superadmin.PostAsJsonAsync("/api/users", request)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await superadmin.PostAsJsonAsync("/api/users", request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignAndRemoveRole_UpdatesRoles()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");

        var users = await (await superadmin.GetAsync("/api/users")).Content.ReadFromJsonAsync<List<UserDto>>();
        var tamu = users!.First(u => u.Email == "tamu@stockmonitor.local");

        var assign = await superadmin.PutAsJsonAsync($"/api/users/{tamu.Id}/roles", new { role = "Supervisi" });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rolesAfter = await (await superadmin.GetAsync($"/api/users/{tamu.Id}/roles")).Content.ReadFromJsonAsync<List<string>>();
        rolesAfter.Should().BeEquivalentTo("Tamu", "Supervisi");

        var remove = await superadmin.DeleteAsync($"/api/users/{tamu.Id}/roles/Supervisi");
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetPassword_OldPasswordStopsWorking()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");

        var users = await (await superadmin.GetAsync("/api/users")).Content.ReadFromJsonAsync<List<UserDto>>();
        var target = users!.First(u => u.Email == "tamu@stockmonitor.local");

        (await superadmin.PutAsJsonAsync($"/api/users/{target.Id}/password", new { newPassword = "Tamu!Baru999" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldLogin = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            email = "tamu@stockmonitor.local",
            password = "Tamu!2345",
        });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLogin = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            email = "tamu@stockmonitor.local",
            password = "Tamu!Baru999",
        });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    internal sealed record UserDto(string Id, string? Email, string? ActiveRole, List<string> Roles);
}
