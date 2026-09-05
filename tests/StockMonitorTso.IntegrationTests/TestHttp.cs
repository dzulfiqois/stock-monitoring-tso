using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using StockMonitorTso.Api;

namespace StockMonitorTso.IntegrationTests;

/// <summary>Helper: client dengan Bearer token dari /api/auth/login (role aktif = role pertama user).</summary>
internal static class TestHttp
{
    internal static async Task<HttpClient> ClientAsync(
        TestApiWebApplicationFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var login = await AuthApiTests.LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }
}
