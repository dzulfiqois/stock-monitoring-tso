using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StockMonitorTso.IntegrationTests;

public class AdminPageAccessTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AdminPageAccessTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminUsers_Unauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("Account/Login");
    }

    [Fact]
    public async Task AdminUsers_SuperadminLogin_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        await LoginAsync(client, "superadmin@stockmonitor.local", "Superadmin!2345");

        var response = await client.GetAsync("/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminUsers_OperatorLogin_Rejected()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        await LoginAsync(client, "operator@stockmonitor.local", "Operator!2345");

        var response = await client.GetAsync("/admin/users");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await loginPage.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(html,
            @"(?:name=""__RequestVerificationToken""[^>]*value=""([^""]+)""|value=""([^""]+)""[^>]*name=""__RequestVerificationToken"")");
        tokenMatch.Success.Should().BeTrue("halaman login harus memuat antiforgery token");
        var token = tokenMatch.Groups[1].Success ? tokenMatch.Groups[1].Value : tokenMatch.Groups[2].Value;

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] = token,
            ["_handler"] = "login",
        });

        var response = await client.PostAsync("/Account/Login", form);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Login POST gagal (400). Potongan body: {body[..Math.Min(body.Length, 500)]}");
        }
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.OK);
    }
}
