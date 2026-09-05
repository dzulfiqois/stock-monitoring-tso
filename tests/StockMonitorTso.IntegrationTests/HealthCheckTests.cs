using System.Net;
using FluentAssertions;

namespace StockMonitorTso.IntegrationTests;

public class HealthCheckTests : IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactory _factory;

    public HealthCheckTests(TestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
