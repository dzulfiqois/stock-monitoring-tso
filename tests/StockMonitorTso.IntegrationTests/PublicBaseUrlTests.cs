using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockMonitorTso.IntegrationTests;

public record RequestSchemeDto(string Scheme, string? Host, bool IsHttps);

public class PublicBaseUrlTests : IClassFixture<TestApiWebApplicationFactoryWithBaseUrl>, IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactoryWithBaseUrl _factoryWithBaseUrl;
    private readonly TestApiWebApplicationFactory _factory;

    public PublicBaseUrlTests(
        TestApiWebApplicationFactoryWithBaseUrl factoryWithBaseUrl,
        TestApiWebApplicationFactory factory)
    {
        _factoryWithBaseUrl = factoryWithBaseUrl;
        _factory = factory;
    }

    [Fact]
    public async Task AppBaseUrl_Set_RequestSchemeAndHostPinnedFromIt()
    {
        var client = _factoryWithBaseUrl.CreateClient();

        var response = await client.GetAsync("/api/debug/request");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RequestSchemeDto>();
        body!.Scheme.Should().Be("https");
        body.IsHttps.Should().BeTrue();
        body.Host.Should().Be("public.example:8443");
    }

    [Fact]
    public async Task AppBaseUrl_Empty_ForwardedProtoHttps_IsHonored()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/debug/request");
        request.Headers.Add("X-Forwarded-Proto", "https");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RequestSchemeDto>();
        body!.Scheme.Should().Be("https");
        body.IsHttps.Should().BeTrue();
    }

    [Fact]
    public async Task AppBaseUrl_Empty_NoForwardedHeaders_StaysHttp()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/debug/request");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RequestSchemeDto>();
        body!.Scheme.Should().Be("http");
        body.IsHttps.Should().BeFalse();
    }
}
