using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class AgenDashboardTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AgenDashboardTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seed_EveryWilayah_HasTwoOrThreeAgen()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var wilayah in WilayahInfo.All)
        {
            var count = await db.Agen.CountAsync(a => a.Wilayah == wilayah && !a.IsDeleted);
            count.Should().BeInRange(2, 3, $"wilayah {wilayah}");
        }
    }

    [Fact]
    public async Task Seed_AgenStokSum_EqualsGudangStokAfterSplit()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var wilayah in WilayahInfo.All)
        {
            foreach (var produk in ProdukInfo.All)
            {
                var gudang = await db.StokEntitas.FirstOrDefaultAsync(
                    e => e.Wilayah == wilayah && e.Produk == produk && e.Tier == Tier.GudangWilayah && !e.IsDeleted);
                if (gudang is null)
                {
                    continue;
                }

                // Gudang menyimpan 50%; sisanya 50% dialihkan ke agen (Σ agen == sisa gudang).
                var agenRows = await db.StokEntitas
                    .Where(e => e.Wilayah == wilayah && e.Produk == produk && e.Tier == Tier.Agen && !e.IsDeleted)
                    .ToListAsync();
                var agenStok = agenRows.Sum(e => e.Stok);
                agenStok.Should().Be(gudang.Stok, $"{wilayah}/{produk}: 50% di gudang, 50% di agen");
            }
        }
    }

    [Fact]
    public async Task Seed_AgenMockTransfer_RecordedInStockTransactions()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var transfers = await db.StockTransactions
            .Where(t => t.Type == StockTransactionType.Transfer && t.Catatan!.Contains("mock 50%"))
            .ToListAsync();

        transfers.Should().NotBeEmpty();
        transfers.Should().OnlyContain(t => t.StokEntitasTujuanId != null);
    }

    [Fact]
    public async Task GetAgenInventaris_Papua_ReturnsAgenRows()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var rows = await dashboard.GetAgenInventarisAsync(Wilayah.Papua);

        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.TotalStok >= 0 && r.JumlahProduk >= 0);
        rows.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetSalesAreaCards_Papua_CardExposesAgenRows()
    {
        using var scope = _factory.Services.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var cards = await dashboard.GetSalesAreaCardsAsync(DashboardFilter.GasLpg);
        var papua = cards.Where(c => c.Wilayah == Wilayah.Papua).ToList();

        papua.Should().NotBeEmpty();
        papua.Should().OnlyContain(c => c.AgenRows.Count >= 2);
    }
}
