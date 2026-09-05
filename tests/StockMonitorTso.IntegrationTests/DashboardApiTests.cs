using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockMonitorTso.IntegrationTests;

public class DashboardApiTests : IClassFixture<TestApiWebApplicationFactoryWithStock>
{
    private readonly TestApiWebApplicationFactoryWithStock _factory;

    public DashboardApiTests(TestApiWebApplicationFactoryWithStock factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Summary_WithSeededStock_ReturnsTotals()
    {
        var client = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.GetAsync("/api/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SummaryDto>();
        body!.TotalStok.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Cards_ReturnsSevenWilayahForEachProductFamily()
    {
        var client = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.GetAsync("/api/dashboard/cards");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cards = await response.Content.ReadFromJsonAsync<List<CardDto>>();
        cards!.GroupBy(c => c.Wilayah).Should().HaveCount(7);
        cards!.Where(c => c.Produk == "MinyakTanah").Should().HaveCount(7);
        cards!.Where(c => c.Produk == "Lpg5_5Kg").Should().HaveCount(7);
    }

    [Fact]
    public async Task Cards_UnknownFilter_Returns400()
    {
        var client = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.GetAsync("/api/dashboard/cards?filter=Nonsense");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LpgDetail_ForWilayah_ReturnsThreeGudangRowsPerSku()
    {
        var client = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.GetAsync("/api/dashboard/sales-area-lpg/Papua");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<LpgDetailDto>();
        detail!.Rows.Should().HaveCount(3);
        detail.Rows.Select(r => r.Produk).Should().BeEquivalentTo("Lpg5_5Kg", "Lpg12Kg", "Lpg50Kg");
        detail.Rows.Select(r => r.Tier).Should().OnlyContain(t => t == "GudangWilayah");
    }

    [Fact]
    public async Task AgenTransferTargets_ForWilayah_ReturnsTargets()
    {
        var client = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.GetAsync("/api/dashboard/agen-transfer-targets/Papua");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var targets = await response.Content.ReadFromJsonAsync<List<TargetDto>>();
        targets!.Should().NotBeEmpty();
        targets.Should().OnlyContain(t => t.Products.Count > 0);
    }

    [Fact]
    public async Task WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    internal sealed record SummaryDto(decimal TotalStok, int ProdukKritis);

    internal sealed record CardDto(string Wilayah, string Produk, decimal TotalStok);

    internal sealed record LpgDetailDto(string Wilayah, List<RowDto> Rows);

    internal sealed record RowDto(string Produk, string Tier, decimal Stok);

    internal sealed record TargetDto(int AgenId, string Nama, List<TargetProductDto> Products);

    internal sealed record TargetProductDto(string Produk, int StokEntitasId);
}
