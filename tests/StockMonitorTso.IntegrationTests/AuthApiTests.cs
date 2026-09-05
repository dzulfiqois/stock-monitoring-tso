using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockMonitorTso.IntegrationTests;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes,
    string Email,
    string ActiveRole,
    string[] Roles);

public record MeResponseDto(string Email, string ActiveRole, string[] Roles);

public sealed record ProblemDto(string Title, int Status, string? Detail);

public class AuthApiTests : IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactory _factory;

    public AuthApiTests(TestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() => _factory.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndActiveRole()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = "superadmin@stockmonitor.local", Password = "Superadmin!2345" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be("superadmin@stockmonitor.local");
        body.ActiveRole.Should().Be("Superadmin");
        body.Roles.Should().Contain("Superadmin");
        body.ExpiresInMinutes.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Problem()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = "superadmin@stockmonitor.local", Password = "wrong-password" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        problem!.Detail.Should().Contain("salah");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = NewClient();

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsUserRolesAndActiveRole()
    {
        var client = NewClient();
        var login = await LoginAsync(client, "multi@stockmonitor.local", "MultiRole!2345");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeResponseDto>();
        me!.Email.Should().Be("multi@stockmonitor.local");
        me.ActiveRole.Should().Be(login.ActiveRole);
        me.Roles.Should().BeEquivalentTo("Operator", "Supervisi", "Tamu");
    }

    [Fact]
    public async Task SwitchRole_AsMultiRoleUser_ReissuesTokenWithNewRole()
    {
        var client = NewClient();
        var login = await LoginAsync(client, "multi@stockmonitor.local", "MultiRole!2345");
        login.ActiveRole.Should().Be("Operator");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/switch-role")
        {
            Content = JsonContent.Create(new { Role = "Supervisi" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var switched = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        switched!.ActiveRole.Should().Be("Supervisi");

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", switched.AccessToken);
        var me = await (await client.SendAsync(meRequest)).Content.ReadFromJsonAsync<MeResponseDto>();
        me!.ActiveRole.Should().Be("Supervisi");
    }

    [Fact]
    public async Task SwitchRole_ToRoleNotOwned_Returns400()
    {
        var client = NewClient();
        var login = await LoginAsync(client, "operator@stockmonitor.local", "Operator!2345");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/switch-role")
        {
            Content = JsonContent.Create(new { Role = "Superadmin" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithRefreshToken_IssuesNewAccessToken()
    {
        var client = NewClient();
        var login = await LoginAsync(client, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = login.RefreshToken });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.ActiveRole.Should().Be("Tamu");
    }

    [Fact]
    public async Task Refresh_WithAccessTokenInstead_Returns401()
    {
        var client = NewClient();
        var login = await LoginAsync(client, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = login.AccessToken });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    internal static async Task<AuthResponseDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!;
    }
}
