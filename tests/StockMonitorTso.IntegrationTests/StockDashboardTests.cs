using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class StockDashboardTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public StockDashboardTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExcelSeed_LoadsLpgGudangWilayahAndOutlet()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var lpgRows = await dashboard.GetLpgRowsAsync();

        lpgRows.Should().NotBeEmpty();
        lpgRows.Should().OnlyContain(r => r.Produk != Produk.MinyakTanah);
    }

    [Fact]
    public async Task ExcelSeed_PapuaBaratDaya5_5Kg_GudangHoldsHalfAndAgenSumHalf()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var rows = await dashboard.GetLpgRowsAsync();
        var row = rows.First(r => r.Wilayah == Wilayah.PapuaBaratDaya && r.Produk == Produk.Lpg5_5Kg);

        // Excel sheet "Agen 16.06.26": stok 1803, DOT 123. Distribusi awal agen (mock 50%)
        // memindahkan 50% stok dari Gudang Wilayah ke agen → gudang menyimpan 901.5.
        row.StokGudang.Should().Be(901.5m);
        row.DotGudang.Should().Be(123);
        row.CdGudang.Should().BeApproximately(901.5m / 123m, 0.01m);
        row.StatusGudang.Should().Be(Status.Aman);
        row.StokAgen.Should().Be(901.5m);
    }

    [Fact]
    public async Task ExcelSeed_PapuaTengah12Kg_StatusWarningAfterSplit()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var rows = await dashboard.GetLpgRowsAsync();
        var row = rows.First(r => r.Wilayah == Wilayah.PapuaTengah && r.Produk == Produk.Lpg12Kg);

        // Excel sheet "Agen 16.06.26": stok 5654, DOT 434. Setelah split 50% → gudang 2827,
        // CD = 2827/434 ≈ 6.51 (Warning).
        row.StokGudang.Should().Be(2827);
        row.DotGudang.Should().Be(434);
        row.CdGudang.Should().BeApproximately(2827m / 434m, 0.01m);
        row.StatusGudang.Should().Be(Status.Warning);
        row.StokAgen.Should().Be(2827);
    }

    [Fact]
    public async Task Summary_ReturnsAggregates()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var summary = await dashboard.GetSummaryAsync();

        summary.TotalStok.Should().BeGreaterThan(0);
        summary.ProdukKritis.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SalesAreaCards_GasLpg_OneCardPerWilayah_WithThreeSkuStok()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var cards = await dashboard.GetSalesAreaCardsAsync(DashboardFilter.GasLpg);

        // 1 card per wilayah (bukan per SKU) untuk Gas LPG.
        cards.Select(c => c.Wilayah).Distinct().Should().HaveCount(cards.Count);
        cards.Should().HaveCount(WilayahInfo.All.Length);
        cards.Should().OnlyContain(c => c.Produk != Produk.MinyakTanah);
        cards.Should().OnlyContain(c => c.StokGudang55Kg.HasValue && c.StokGudang12Kg.HasValue && c.StokGudang50Kg.HasValue,
            "tiap card gas memuat rincian 3 ukuran");
    }

    [Fact]
    public async Task LpgDetail_Papua_ReturnsSixRows_WithPerSkuBreakdown()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var detail = await dashboard.GetLpgDetailAsync(Wilayah.Papua);

        detail.Should().NotBeNull();
        detail!.Rows.Should().HaveCount(6); // 3 ukuran × (Gudang Wilayah + Outlet)
        detail.Rows.Select(r => r.Produk).Should().Contain(new[] { Produk.Lpg5_5Kg, Produk.Lpg12Kg, Produk.Lpg50Kg });
        detail.Rows.Select(r => r.Tier).Should().Contain(new[] { Tier.GudangWilayah, Tier.Outlet });
        detail.Rows.Should().OnlyContain(r => r.StokEntitasId > 0);
    }
}
